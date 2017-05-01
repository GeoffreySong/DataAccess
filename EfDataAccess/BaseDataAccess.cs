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

		public List<T> GetAll()
		{
			var expre = CheckDataFilter<T>();

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				if (expre != null)
				{
					return context.Set<T>().AsNoTracking().Where(expre).ToList();
				}
				else
				{
					return context.Set<T>().AsNoTracking().ToList();
				}
			}
		}

		public List<T> GetAll(string include)
		{
			var expre = CheckDataFilter<T>();

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;
				if (expre != null)
				{
					return context.Set<T>().Include(include).AsNoTracking().Where(expre).ToList();
				}
				else
				{
					return context.Set<T>().Include(include).AsNoTracking().ToList();
				}
			}
		}

		public List<T> GetAll(string[] includes)
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

		public List<T> Find(Expression<Func<T, bool>> predicate)
		{
			Expression<Func<T, bool>> expre = CheckDataFilter<T>(predicate.ToString());

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				if (expre != null)
				{
					return context.Set<T>().AsNoTracking().Where(predicate).Where(expre).ToList();
				}
				else
				{
					return context.Set<T>().AsNoTracking().Where(predicate).ToList();
				}
			}
		}

		public List<T> Find(Expression<Func<T, bool>> predicate, string include)
		{
			Expression<Func<T, bool>> expre = CheckDataFilter<T>(predicate.ToString());

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				if (expre != null)
				{
					return context.Set<T>().Include(include).AsNoTracking().Where(predicate).Where(expre).ToList();
				}
				else
				{
					return context.Set<T>().Include(include).AsNoTracking().Where(predicate).ToList();
				}

			}
		}

		public List<T> Find(Expression<Func<T, bool>> predicate, string[] includes)
		{
			Expression<Func<T, bool>> expre = CheckDataFilter<T>(predicate.ToString());

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
					return query.AsNoTracking().Where(predicate).Where(expre).ToList();
				}
				else
				{
					return query.AsNoTracking().Where(predicate).ToList();
				}
			}
		}

		public List<T> Find(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> include)
		{
			Expression<Func<T, bool>> expre = CheckDataFilter<T>(predicate.ToString());

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				var query = context.Set<T>() as IQueryable<T>;

				if (expre != null)
				{
					return query.Include(include).AsNoTracking().Where(predicate).Where(expre).ToList();
				}
				else
				{
					return query.Include(include).AsNoTracking().Where(predicate).ToList();
				}
			}
		}

		public List<T> Find(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
		{
			Expression<Func<T, bool>> expre = CheckDataFilter<T>(predicate.ToString());

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;

				var query = context.Set<T>() as IQueryable<T>;

				query = includes.Aggregate(query, (current, include) => current.Include(include));

				if (expre != null)
				{
					return query.AsNoTracking().Where(predicate).Where(expre).ToList();
				}
				else
				{
					return query.AsNoTracking().Where(predicate).ToList();
				}
			}
		}

		public T Add(T entity)
		{
			return Add(entity);
		}

		public T Add(T entity, int userId)
		{
			if (entity == null) return default(T);

			var now = DateTime.UtcNow;
			using (var context = new TContext())
			{
				context.Set<T>().Add(entity);
				var entry = context.Entry(entity);
				SetCreatedByFields(entry, entity.GetType(), now, userId.ToString());
				SetUpdatedByFields(entry, entity.GetType(), now, userId.ToString());
				context.SaveChanges();
			}
			
			return entity;
		}

		public List<T> AddRange(IEnumerable<T> entities)
		{
			return AddRange(entities);
		}

		public List<T> AddRange(IEnumerable<T> entities, int userId)
		{
			var entityList = entities.ToList();

			if (!entityList.Any()) return default(List<T>);

			var now = DateTime.UtcNow;

			entityList.ForEach(a => SetDefaultFields(a, now, userId.ToString()));

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
				context.Entry(entity).State = EntityState.Added;
				Detach(context, entity, new HashSet<object>(), true);
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
					context.Entry(entity).State = EntityState.Added;
					Detach(context, entity, new HashSet<object>(), true);
					context.Entry(entity).State = EntityState.Deleted;
				}

				context.SaveChanges();
			}			
		}

		public void Update(T entity)
		{
			Update(entity);
		}

		public void Update(T entity, int userId)
		{
			if (entity == null) return;

			using (var context = new TContext())
			{
				context.Configuration.ProxyCreationEnabled = false;
				context.Entry(entity).State = EntityState.Added;

				CheckModified(context, entity, userId);
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

		/*******Private methods********************************************************************/

		private List<object> _added = new List<object>();
		private List<object> _deleted = new List<object>();
		private List<Tuple<object, object>> _updated = new List<Tuple<object, object>>();

		private void CheckModified(DbContext context, object entity, int userId, bool detachEnabled = false)
		{
			var hashSet = new HashSet<object>();
			hashSet.Add(entity);
			CheckModified(context, entity, hashSet, userId, detachEnabled);
		}


		private void CheckModified(DbContext context, object entity, HashSet<object> hashSet, int userId, bool detachEnabled = false)
		{
			if (entity == null) return;

			var entry = context.Entry(entity);
			var now = DateTime.UtcNow;
			var type = entity.GetType();

			entry.State = EntityState.Unchanged;
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
					SetUpdatedByFields(entry, type, now, userId.ToString());
					_updated.Add(new Tuple<object, object>(origianlValues.ToObject(), entity));
				}
			}
			else
			{
				entry.State = EntityState.Added;
				SetCreatedByFields(entry, type, now, userId.ToString());
				SetUpdatedByFields(entry, type, now, userId.ToString());
				_added.Add(entry.Entity);
			}

			foreach (var prop in type.GetProperties().Where(a => !a.PropertyType.IsValueType && a.PropertyType.Name != "String"))
			{
				if (prop.GetCustomAttributes(typeof(NotMappedAttribute), false).Length > 0) continue;
				var member = entry.Member(prop.Name);

				if (member is DbReferenceEntry)
				{
					if (detachEnabled)
					{
						Detach(context, member.CurrentValue, hashSet);
					}
					else
					{
						CheckModified(context, member.CurrentValue, hashSet, userId, true);
					}
				}
				else if (member is DbCollectionEntry)
				{
					var collection = prop.GetValue(entity, null) as IEnumerable<object>;
					if (collection != null)
					{
						var list = collection.ToList();
						for (var i = 0; i < list.Count; i++)
						{
							var ent = list[i];
							if (detachEnabled)
							{
								Detach(context, ent, hashSet);
							}
							else
							{
								var naviEntry = context.Entry(ent);
								var naviType = ent.GetType();
								if (naviType.GetProperty("Id") != null && naviEntry.Member("Id").CurrentValue != null)
								{
									naviEntry.State = EntityState.Added;
									SetCreatedByFields(naviEntry, naviType, now, userId.ToString());
									SetUpdatedByFields(naviEntry, naviType, now, userId.ToString());
									_added.Add(ent);
								}

								Detach(context, ent, hashSet, true);
							}
						}
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

		private void SetCreatedByFields(DbEntityEntry entry, Type type, DateTime date, String user)
		{
			var CRTDT = "CreatedDate";
			var CRTBY = "CreatedBy";

			if (type.GetProperty(CRTDT) != null)
			{
				entry.Member(CRTDT).CurrentValue = date;
			}

			if (type.GetProperty(CRTBY) != null)
			{
				entry.Member(CRTBY).CurrentValue = user ?? "SYSTEM";
			}
		}

		private void SetDefaultFields(object entry, DateTime date, string user)
		{
			var CRTDT = "CreatedDate";
			var CRTBY = "CreatedBy";
			var UPTDT = "UpdatedDate";
			var UPTBY = "UpdatedBy";

			var type = entry.GetType();

			foreach (var prop in type.GetProperties().Where(a => a.PropertyType.IsValueType || a.PropertyType.Name == "String"))
			{
				if (prop.Name == CRTDT || prop.Name == UPTDT) prop.SetValue(entry, date);
				if (prop.Name == CRTBY || prop.Name == UPTBY) prop.SetValue(entry, user);
			}
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

		private void SetUpdatedByFields(object entity, DateTime date, string user)
		{
			var UPTDT = "UpdatedDate";
			var UPTBY = "UpdatedBy";

			var propertyToSet = entity.GetType().GetProperty(UPTDT);
			if (propertyToSet != null)
			{
				propertyToSet.SetValue(entity, date, null);
			}
			propertyToSet = entity.GetType().GetProperty(UPTBY);
			if (propertyToSet != null)
			{
				propertyToSet.SetValue(entity, user, null);
			}
		}

		private void SetUpdatedByFields(DbEntityEntry entry, Type type, DateTime date, String user)
		{
			var UPTDT = "UpdatedDate";
			var UPTBY = "UpdatedBy";
			if (type.GetProperty(UPTDT) != null)
			{
				entry.Member(UPTDT).CurrentValue = date;
			}
			if (type.GetProperty(UPTBY) != null)
			{
				entry.Member(UPTBY).CurrentValue = user ?? "SYSTEM";
			}
		}

		private Expression<Func<TEntity, bool>> CheckDataFilter<TEntity>(string predicate = null)
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

		private const string USERID = "UserId";
		private const string COMPANYID = "CompanyId";
		private const string FIID = "InstitutionId";

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
	}
}

