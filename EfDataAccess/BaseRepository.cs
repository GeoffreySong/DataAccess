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
	/// <summary>
	/// Derived class for specific DbContext is mainly used for saving purpose.
	/// lazy loading of related data is disabled	/// 
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	public class BaseRepository<TContext> : IBaseRepository, IDisposable where TContext : DbContext, new()
	{
		protected readonly TContext _context;

		public BaseRepository(TContext context)	{ _context = context; _context.Configuration.ProxyCreationEnabled = false; }

		public BaseRepository() { _context = new TContext(); _context.Configuration.ProxyCreationEnabled = false; }

		~BaseRepository() { if (_context != null) _context.Dispose(); }

		public T StartTracking<T>(T entity) where T: class
		{
			_context.Set<T>().Attach(entity);
			return entity;
		}

		public T EndTracking<T>(T entity) where T : class
		{
			var dbEntry = _context.Entry(entity);
			if (dbEntry != null) dbEntry.State = EntityState.Detached;
			return entity;
		}

		public T Get<T>(int id) where T : class
		{
			return _context.Set<T>().Find(id);
		}

		public List<T> GetAll<T>(params string[] includes) where T : class
		{
			var expre = CheckDataFilter<T>();
			var query = _context.Set<T>() as IQueryable<T>;

			foreach (var str in includes)
			{
				query = query.Include(str);
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

		public List<T> Find<T>(Expression<Func<T, bool>> predicate, params string[] includes) where T : class
		{
			return Filter(new Expression<Func<T, bool>>[] { predicate }, includes).ToList();
		}

		public List<T> Find<T>(Expression<Func<T, bool>>[] predicates, params string[] includes) where T : class
		{
			return Filter(predicates, includes).ToList();
		}

		public bool Any<T>(Expression<Func<T, bool>> predicate) where T : class
		{
			return Filter(new Expression<Func<T, bool>>[] { predicate }).Any();
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
			_context.SaveChanges();			
		}

		public void SaveChanges(int userId)
		{
			var now = DateTime.UtcNow;
			foreach (var entry in _context.ChangeTracker.Entries().Where(a => a.State == EntityState.Added))
			{
				SetCreatedFields(entry, now, userId);
				SetCreatedFields(entry, now, userId);
			}

			foreach (var entry in _context.ChangeTracker.Entries().Where(a => a.State == EntityState.Modified))
			{
				SetCreatedFields(entry, now, userId);
			}
		}

		public int GetSequenceNumber(string seqObject)
		{
			return _context.Database.SqlQuery<int>("SELECT NEXT VALUE FOR dbo." + seqObject + ";").FirstOrDefault();
		}

		/********private methods********************************************************/
		private const string CRTDT = "CreatedDate";
		private const string CRTBY = "CreatedBy";
		private const string UPTDT = "UpdatedDate";
		private const string UPTBY = "UpdatedBy";

		private IQueryable<T> Filter<T>(Expression<Func<T, bool>>[] predicates, params string[] includes) where T : class
		{
			var expre = CheckDataFilter<T>();
			var query = _context.Set<T>() as IQueryable<T>;
			foreach (var str in includes)
			{
				query = query.Include(str);
			}

			foreach (var p in predicates)
			{
				query = query.Where(p);
			}

			if (expre != null)
			{
				return query.Where(expre);
			}
			else
			{
				return query;
			}
		}

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

		private void SetCreatedFields(System.Data.Entity.Infrastructure.DbEntityEntry entry, DateTime createdDate, int createdBy)
		{
			var type = entry.Entity.GetType();
			if (type.GetProperty(CRTDT) != null) entry.Member(CRTDT).CurrentValue = createdDate;
			if (type.GetProperty(CRTBY) != null) entry.Member(CRTBY).CurrentValue = createdBy;
		}

		private void SetUpdatedFields(System.Data.Entity.Infrastructure.DbEntityEntry entry, DateTime updatedDate, int updatedBy)
		{
			var type = entry.Entity.GetType();
			if (type.GetProperty(UPTDT) != null) entry.Member(UPTDT).CurrentValue = updatedDate;
			if (type.GetProperty(UPTBY) != null) entry.Member(UPTBY).CurrentValue = updatedBy;
		}
	}
}

