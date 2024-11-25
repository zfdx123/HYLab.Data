using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HYLab.Data.DBase
{
	public interface BaseDatabase
	{
		Task OpenConnectionAsync();
		void CloseConnection();
		Task<List<T>> Select<T>(string query, object[] parameters) where T : new();
		Task<bool> Insert<T>(T item, string insertQuery) where T : new();
		Task<bool> Update<T>(T item, string updateQuery) where T : new();
		Task<bool> Delete<T>(T item, string deleteQuery) where T : new();
		Task<object?> ExecuteScalarAsync(string query, object[]? parameters);
		Task<int> ExecuteNonQueryAsync(string query, object[]? parameters = null);
		Task<int> ExecuteNonQueryAsync(string query, object[]? parameters, IDbTransaction? transaction);
		Task<bool> ExecuteTransactionAsync(params Func<IDbTransaction, Task<int>>[] operations);
		object[] CreateParameters<T>(T item);
	}

}
