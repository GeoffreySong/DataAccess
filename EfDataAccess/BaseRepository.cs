using Dal.Interface;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EfDataAccess
{
	public class BaseRepository<TContext> : IBaseRepository, IDisposable where TContext : DbContext, new()
	{
		protected readonly TContext _context;

		public BaseRepository(TContext context)
		{
			_context = context; _context.Configuration.ProxyCreationEnabled = false;
		}

		public BaseRepository() { _context = new TContext(); _context.Configuration.ProxyCreationEnabled = false; }

		public T Get<T>(int id) where T : class
		{
			return _context.Set<T>().Find(id);
		}

		public List<T> GetAll<T>(string[] includes = null) where T : class
		{
			var expre = CheckDataFilter<T>();
			var query = _context.Set<T>() as IQueryable<T>;
			if (includes != null)
			{
				foreach (var str in includes)
				{
					query = query.Include(str);
				}
			}
			if (expre != null)
			{
				return query.Where(expre).ToList();
			}
			else
			{
				return query.ToList();
			}
		}

		public List<T> Find<T>(Expression<Func<T, bool>> predicate, string[] includes = null) where T : class
		{
			return FindInternal(predicate, includes).ToList();
		}

		public bool Any<T>(Expression<Func<T, bool>> predicate) where T : class
		{
			return FindInternal(predicate).Any();
		}

		private IQueryable<T> FindInternal<T>(Expression<Func<T, bool>> predicate, string[] includes = null) where T : class
		{
			var expre = CheckDataFilter<T>();
			var query = _context.Set<T>() as IQueryable<T>;
			if (includes != null)
			{
				foreach (var str in includes)
				{
					query = query.Include(str);
				}
			}
			if (expre != null)
			{
				return query.Where(predicate).Where(expre);
			}
			else
			{
				return query.Where(predicate);
			}
		}

		public List<T> Filter<T>(Expression<Func<T, bool>>[] predicates, string[] includes = null) where T : class
		{
			var query = _context.Set<T>() as IQueryable<T>;
			if (includes != null)
			{
				foreach (var str in includes)
				{
					query = query.Include(str);
				}
			}
			foreach (var predicate in predicates)
			{
				query = query.Where(predicate);
			}
			return query.ToList();
		}

		public void Add<T>(T entity) where T : class
		{
			if (entity != null) _context.Set<T>().Add(entity);
		}

		public void AddRange<T>(IEnumerable<T> entities) where T : class
		{
			if (entities != null && entities.Any()) _context.Set<T>().AddRange(entities);
		}

		public void Remove<T>(T entity) where T : class
		{
			if (entity != null) _context.Set<T>().Remove(entity);
		}

		public void RemoveRange<T>(IEnumerable<T> entities) where T : class
		{
			if (entities != null && entities.Any()) _context.Set<T>().RemoveRange(entities);
		}

		public void Dispose()
		{
			_context.Dispose();
		}

		public void SaveChanges()
		{
			SaveChanges();
		}

		public void SaveChanges(int userId)
		{
			DateTime now = DateTime.UtcNow;
			foreach (var entry in _context.ChangeTracker.Entries().Where(a => a.State == EntityState.Added))
			{
				SetValueOfProperty(entry.Entity, CRTDT, now);
				SetValueOfProperty(entry.Entity, CRTBY, userId.ToString());
				SetValueOfProperty(entry.Entity, UPTDT, now);
				SetValueOfProperty(entry.Entity, UPTBY, userId.ToString());
			}
			foreach (var entry in _context.ChangeTracker.Entries().Where(a => a.State == EntityState.Modified))
			{
				SetValueOfProperty(entry.Entity, UPTDT, now);
				SetValueOfProperty(entry.Entity, UPTBY, userId.ToString());
			}

			var errors = _context.GetValidationErrors();
			if (errors.Any())
			{
				var errMsg = String.Empty;
				foreach (var result in errors)
				{
					var entity = result.Entry.Entity;
					var name = entity.GetType().Name;
					var str = string.Join(", ", result.ValidationErrors.Select(a => a.ErrorMessage));
					errMsg += name + ": " + str + Environment.NewLine;
				}

				throw new DbEntityValidationException(errMsg);
			}			
		}

		public int GetSequenceNumber(string seqObject)
		{
			return _context.Database.SqlQuery<int>("SELECT NEXT VALUE FOR dbo." + seqObject + ";").FirstOrDefault();
		}

		public string GetUserName(int userId)
		{
			using (var context = new TContext())
			{
				return context.Database.SqlQuery<string>("SELECT NAME FROM dbo.[User] Where UserId =" + userId + " ;").FirstOrDefault();
			}
		}

		public string GetAccountNumber(int accountId)
		{
			using (var context = new TContext())
			{
				return context.Database.SqlQuery<string>("SELECT Number FROM dbo.Account Where AccountId =" + accountId + " ;").FirstOrDefault();
			}
		}

		/********private methods********************************************************/
		private const string CRTDT = "CreatedDate";
		private const string CRTBY = "CreatedBy";
		private const string UPTDT = "UpdatedDate";
		private const string UPTBY = "UpdatedBy";
		
		private Expression<Func<TEntity, bool>> CheckDataFilter<TEntity>()
		{
			return null;
		}

		private void GetAudits(DbContext context, out List<object> added, out List<object> deleted, out List<Tuple<object, object>> updated)
		{
			added = context.ChangeTracker.Entries().Where(a => a.State == EntityState.Added).Select(b => b.Entity).ToList();
			deleted = context.ChangeTracker.Entries().Where(a => a.State == EntityState.Deleted).Select(b => b.Entity).ToList();
			updated = context.ChangeTracker.Entries().Where(a => a.State == EntityState.Modified)
				.Select(a => new Tuple<object, object>(a.OriginalValues.ToObject(), a.CurrentValues.ToObject())).ToList();

		}

		private bool SetValueOfProperty(object entity, string property, object value)
		{
			var propertyToSet = entity.GetType().GetProperty(property);
			if (propertyToSet != null)
			{
				propertyToSet.SetValue(entity, value, null);
				return true;
			}
			return false;
		}

	}
}

