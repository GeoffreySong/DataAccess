using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dal.Interface
{
	public interface IBaseRepository : IDisposable
	{
		T Get<T>(int id) where T : class;
		List<T> GetAll<T>(string[] includes = null) where T : class;
		List<T> Find<T>(Expression<Func<T, bool>> predicate, string[] includes = null) where T : class;
		bool Any<T>(Expression<Func<T, bool>> predicate) where T : class;
		List<T> Filter<T>(Expression<Func<T, bool>>[] predicates, string[] includes = null) where T : class;
		void Add<T>(T entity) where T : class;
		void AddRange<T>(IEnumerable<T> entities) where T : class;
		void Remove<T>(T entity) where T : class;
		void RemoveRange<T>(IEnumerable<T> entities) where T : class;
		void SaveChanges();
		void SaveChanges(int userId);
		int GetSequenceNumber(string seqObject);
		string GetUserName(int userId);
		string GetAccountNumber(int accountId);
	}
}
