using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dal.Interface
{
	public interface IBaseRepository : IDisposable
	{
		T StartTracking<T>(T entity) where T : class;
		T EndTracking<T>(T entity) where T : class;
		T Get<T>(int id) where T : class;
		List<T> GetAll<T>(params string[] includes) where T : class;
		List<T> Find<T>(Expression<Func<T, bool>> predicate, params string[] includes) where T : class;
		List<T> Find<T>(Expression<Func<T, bool>>[] predicates, params string[] includes) where T : class;
		bool Any<T>(Expression<Func<T, bool>> predicate) where T : class;
		void Add<T>(T entity) where T : class;
		void AddRange<T>(IEnumerable<T> entities) where T : class;
		void Remove<T>(T entity) where T : class;
		void RemoveRange<T>(IEnumerable<T> entities) where T : class;
		void SaveChanges();
		void SaveChanges(int userId);
		int GetSequenceNumber(string seqObject);		
	}
}
