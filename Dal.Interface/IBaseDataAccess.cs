using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dal.Interface
{
	public interface IBaseDataAccess<T> where T : class
	{
		T Get(int id);
		List<T> GetAll(params string[] includes);
		List<T> Find(Expression<Func<T, bool>> predicate, params string[] includes);
		List<T> Find(Expression<Func<T, bool>>[] predicates, params string[] includes);
		List<T> Find(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
		List<T> Find(Expression<Func<T, bool>>[] predicateS, params Expression<Func<T, object>>[] includes);
		T Add(T entity);
		T Add(T entity, int userId);
		List<T> AddRange(IEnumerable<T> entities);
		List<T> AddRange(IEnumerable<T> entities, int userId);
		void Remove(T entity);
		void RemoveRange(IEnumerable<T> entities);
		void Update(T entity);
		void Update(T entity, int userId);
		int GetSequenceNumber(string seqObject);
		List<T> ExecuteProc(string procName, params dynamic[] parameters);
	}
}

