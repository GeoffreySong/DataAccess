using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq.Expressions;
using System.Linq;
using Dal.Interface;

namespace EfDataAccess
{
	public abstract class BaseDataAccess<T, TContext> : IBaseDataAccess<T>	
		where T : class
		where TContext : DbContext, new()
	{
		public T Get(int id)
		{
			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				return context.Set<T>().Find(id);
			}
		}

		public List<T> GetAll(params string[] includes)
		{
			var expre = CheckDataFilter<T>();

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;
				var query = context.Set<T>() as IQueryable<T>;

				foreach (var str in includes)
				{
					query = query.Include(str);
				}

				if (expre != null)
				{
					return query.AsNoTracking().Where(expre).ToList();
				}
				else
				{
					return query.AsNoTracking().ToList();
				}
			}
		}

		public List<T> Find(Expression<Func<T, bool>> predicate, params string[] includes)
		{
			return Find(new Expression<Func<T, bool>>[] { predicate }, includes);
		}

		public List<T> Find(Expression<Func<T, bool>>[] predicates, params string[] includes)
		{
			var expre = CheckDataFilter<T>();

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				var query = context.Set<T>() as IQueryable<T>;

				foreach (var p in predicates)
				{
					query = query.Where(p);
				}

				foreach (var str in includes)
				{
					query = query.Include(str);
				}
								
				if (expre != null)
				{
					return query.AsNoTracking().Where(expre).ToList();
				}
				else
				{
					return query.AsNoTracking().ToList();
				}
			}
		}	

		public List<T> Find(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
		{
			return Find(new Expression<Func<T, bool>>[] { predicate }, includes);			
		}

		public List<T> Find(Expression<Func<T, bool>>[] predicates, params Expression<Func<T, object>>[] includes)
		{
			var expre = CheckDataFilter<T>();

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				var query = context.Set<T>() as IQueryable<T>;

				foreach (var p in predicates)
				{
					query = query.Where(p);
				}

				query = includes.Aggregate(query, (current, include) => current.Include(include));

				if (expre != null)
				{
					return query.AsNoTracking().Where(expre).ToList();
				}
				else
				{
					return query.AsNoTracking().ToList();
				}
			}
		}

		public T Add(T entity)
		{
			return Add(entity, 0);
		}

		public T Add(T entity, int userId)
		{
			if (entity == null) return default(T);

			var now = DateTime.UtcNow;
			SetDefaultFields(entity, now, userId);

			using (var context = new TContext())
			{
				context.Set<T>().Add(entity);
				context.SaveChanges();
			}
			
			return entity;
		}

		public List<T> AddRange(IEnumerable<T> entities)
		{
			return AddRange(entities, 0);
		}

		public List<T> AddRange(IEnumerable<T> entities, int userId)
		{
			var entityList = entities.ToList();

			if (!entityList.Any()) return default(List<T>);

			var now = DateTime.UtcNow;

			entityList.ForEach(a => SetDefaultFields(a, now, userId));				

			using (var context = new TContext())
			{
				context.Set<T>().AddRange(entityList);
				context.SaveChanges();
			}

			return entityList;
		}

		public void Remove(T entity)
		{
			if (entity == null) return;

			using (var context = new TContext())
			{
				context.Entry(entity).State = EntityState.Deleted;
				context.SaveChanges();
			}			
		}

		public void RemoveRange(IEnumerable<T> entities)
		{
			if (entities == null || !entities.Any()) return;

			using (var context = new TContext())
			{
				foreach (var entity in entities)
				{
					context.Entry(entity).State = EntityState.Deleted;
				}

				context.SaveChanges();
			}			
		}

		public void Update(T entity)
		{
			Update(entity, 0);
		}

		public void Update(T entity, int userId)
		{
			if (entity == null) return;

			using (var context = new TContext())
			{
				context.Set<T>().Attach(entity);
				var now = DateTime.UtcNow;
				CheckModified(context, entity, now, userId);
				context.SaveChanges();				
			}
		}

		public int GetSequenceNumber(string seqObject)
		{
			using (var context = new TContext())
			{
				return context.Database.SqlQuery<int>("SELECT NEXT VALUE FOR dbo." + seqObject + ";").FirstOrDefault();
			}
		}

		public List<T> ExecuteProc(string proc, params dynamic[] parameters)
		{
			using (var context = new TContext())
			{
				return context.Database.SqlQuery<T>(proc, parameters).ToList();
			}
		}

		public Tuple<List<T1>, List<T2>> ExecuteProcedure<T1, T2>(string procedure, params dynamic[] parameters)
		{
			Tuple<List<T1>, List<T2>> rtn = null;
			using (var context = new TContext())
			{
				var cmd = context.Database.Connection.CreateCommand();
				cmd.CommandText = procedure;
				foreach (var p in parameters)
				{
					cmd.Parameters.Add(p);
				}
				
				try
				{
					context.Database.Connection.Open();
					var reader = cmd.ExecuteReader();
					var item1 = ((IObjectContextAdapter)context).ObjectContext.Translate<T1>(reader, typeof(T1).Name, System.Data.Entity.Core.Objects.MergeOption.AppendOnly).ToList();
					reader.NextResult();
					var item2 = ((IObjectContextAdapter)context).ObjectContext.Translate<T2>(reader, typeof(T2).Name, System.Data.Entity.Core.Objects.MergeOption.AppendOnly).ToList();

					rtn = new Tuple<List<T1>, List<T2>> (item1, item2);
				}
				finally
				{
					context.Database.Connection.Close();
				}
			}

			return rtn;
		}

		/*******Private methods********************************************************************/

		private readonly string CRTDT = "CreatedDate";
		private readonly string CRTBY = "CreatedBy";
		private readonly string UPTDT = "UpdatedDate";
		private readonly string UPTBY = "UpdatedBy";

		private void CheckModified(DbContext context, object entity, DateTime now, int userId)
		{
			if (entity == null) return;

			var entry = context.Entry(entity);
			var type = entity.GetType();

			var origianlValues = entry.GetDatabaseValues();

			if (origianlValues != null)
			{
				foreach (var prop in origianlValues.PropertyNames.Where(a => a != "Id"))
				{
					var origValue = origianlValues.GetValue<object>(prop);
					var dbPropEntry = entry.Property(prop);
					var currValue = dbPropEntry.CurrentValue;

					if (!ValuesEqual(origValue, currValue)) dbPropEntry.IsModified = true;
				}

				if (entry.State == EntityState.Modified)
				{
					SetUpdatedByFields(entry, type, now, userId);
				}
			}
			else                     //Update() can be called for Adding
			{
				entry.State = EntityState.Added;
				SetCreatedByFields(entry, type, now, userId);
				SetUpdatedByFields(entry, type, now, userId);
			}

			foreach (var prop in type.GetProperties().Where(a => !a.PropertyType.IsValueType && a.PropertyType.Name != "String"))
			{
				if (prop.GetCustomAttributes(typeof(NotMappedAttribute), false).Length > 0) continue;
				var member = entry.Member(prop.Name);

				if (member is DbReferenceEntry)
				{
					CheckModified(context, member.CurrentValue, now, userId);
				}
				else if (member is DbCollectionEntry)
				{
					var collection = prop.GetValue(entity, null) as IEnumerable<object>;
					if (collection != null)
					{
						foreach (var ent in collection)
						{
							CheckModified(context, ent, now, userId);							
						}						
					}
				}
			}
		}

		private void SetDefaultFields(object entity, DateTime date, int userId)
		{
			var type = entity.GetType();

			foreach (var prop in type.GetProperties().Where(a => a.PropertyType.IsValueType || a.PropertyType.Name == "int32"))
			{
				if (prop.Name == CRTDT || prop.Name == UPTDT) prop.SetValue(entity, date);
				if (prop.Name == CRTBY || prop.Name == UPTBY) prop.SetValue(entity, userId);
			}
		}

		private void SetCreatedByFields(DbEntityEntry entry, Type type, DateTime date, int userId)
		{
			if (type.GetProperty(CRTDT) != null)
			{
				entry.Member(CRTDT).CurrentValue = date;
			}
			if (type.GetProperty(CRTBY) != null)
			{
				entry.Member(CRTBY).CurrentValue = userId;
			}
		}
		private void SetUpdatedByFields(DbEntityEntry entry, Type type, DateTime date, int userId)
		{
			
			if (type.GetProperty(UPTDT) != null)
			{
				entry.Member(UPTDT).CurrentValue = date;
			}
			if (type.GetProperty(UPTBY) != null)
			{
				entry.Member(UPTBY).CurrentValue = userId;
			}
		}

		private Expression<Func<TEntity, bool>> CheckDataFilter<TEntity>()
		{
			return null;
		}

		private bool ValuesEqual<TValue>(TValue valueA, TValue valueB)
		{
			if (valueA != null)
			{
				if (valueB == null) return false;

				if (valueA.GetType().IsArray)
				{
					var arrayA = (valueA as Array).Cast<object>();
					var arrayB = (valueB as Array).Cast<object>();
					return arrayA.SequenceEqual(arrayB);
				}
			}

			return EqualityComparer<TValue>.Default.Equals(valueA, valueB);
		}

		/******Not interface methods*********************************************************************/
		public void Update(int id, T entity)
		{
			using (var context = new TContext())
			{
				var target = context.Set<T>().Find(id);
				CopyProperties(entity, target);
				context.SaveChanges();
			}
		}

		private void CopyProperties(object source, object target)
		{
			if (source == null) return;

			if (target == null)
			{
				target = source;
			}
			else
			{
				var sourceType = source.GetType();
				var propInfos = sourceType.GetProperties();

				foreach (var prop in propInfos)
				{
					if (!prop.CanRead || !prop.CanWrite) continue;

					var type = prop.PropertyType;
					if (type.IsPrimitive || type.IsValueType || type == typeof(string))
					{
						prop.SetValue(target, prop.GetValue(source, null), null);
					}
					else if (typeof(IEnumerable).IsAssignableFrom(type))
					{

					}
					else
					{
						CopyProperties(prop.GetValue(source, null), prop.GetValue(target, null));
					}
				}
			}
		}

		private void Detach(DbContext context, object entity, ISet<object> detachedSet, bool isRoot = false)
		{
			if (entity == null) return;
			if (!detachedSet.Add(entity)) return; //this is to prevent infinite recursion

			var entry = context.Entry(entity);
			foreach (var prop in entity.GetType().GetProperties().Where(a => !a.PropertyType.IsValueType && a.PropertyType.Name != "String"))
			{
				var member = entry.Member(prop.Name);

				if (member is DbReferenceEntry)
				{
					Detach(context, prop.GetValue(entity, null), detachedSet);
				}
				else if (member is DbCollectionEntry)
				{
					var collection = prop.GetValue(entity, null) as IEnumerable<object>;
					if (collection == null) continue;
					foreach (var child in collection.ToList())
					{
						Detach(context, child, detachedSet);
					}
				}
			}
			if (!isRoot) entry.State = EntityState.Detached;
		}
	}
}

