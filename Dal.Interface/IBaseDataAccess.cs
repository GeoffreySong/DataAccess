using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dal.Interface
{
	public interface IBaseDataAccess<T> where T : class
	{
		T Get(int id);
		List<T> GetAll();
		List<T> GetAll(string include);
		List<T> GetAll(string[] includes);
		List<T> Find(Expression<Func<T, bool>> predicate);
		List<T> Find(Expression<Func<T, bool>> predicate, string include);
		List<T> Find(Expression<Func<T, bool>> predicate, string[] includes);
		List<T> Find(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> include);
		List<T> Find(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
		T Add(T entity);
		T Add(T entity, int userId);
		List<T> AddRange(IEnumerable<T> entities);
		List<T> AddRange(IEnumerable<T> entities, int userId);
		void Remove(T entity);
		void RemoveRange(IEnumerable<T> entities);
		void Update(T entity);
		void Update(T entity, int userId);
		int GetSequenceNumber(string seqObject);
		string GetUserName(int userId);
		string GetAccountNumber(int accountId);
	}
}

