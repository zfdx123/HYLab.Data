using HYLab.Data.DBase;
using System;
using System.Collections.Generic;
using System.Data;
#if NET8_0
using Microsoft.Data.SqlClient;
#elif NET481
using System.Data.SqlClient;
#endif
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace HYLab.Data.SqlServer
{
	public class SqlServerDatabase : BaseDatabase
	{
		private readonly SqlConnection _connection;

		public SqlServerDatabase(string connectionString)
		{
			_connection = new SqlConnection(connectionString);
		}

		public async Task OpenConnectionAsync()
		{
			if (_connection.State != ConnectionState.Open)
				await _connection.OpenAsync();
		}

		public void CloseConnection()
		{
			if (_connection.State == ConnectionState.Open)
				_connection.Close();
		}

		public async Task<List<T>> Select<T>(string query, object[] parameters) where T : new()
		{
			await OpenConnectionAsync();

			using (SqlCommand sqlCommand = new SqlCommand(query, _connection))
			{
				if (parameters != null)
					sqlCommand.Parameters.AddRange(parameters);

				using (SqlDataAdapter adapter = new SqlDataAdapter(sqlCommand))
				{
					DataSet ds = new DataSet();
					adapter.Fill(ds);

					List<T> results = new List<T>();
					foreach (DataTable table in ds.Tables)
					{
						foreach (DataRow row in table.Rows)
						{
							T obj = new T();
							foreach (DataColumn column in table.Columns)
							{
								var property = typeof(T).GetProperty(column.ColumnName);
								if (property != null && row[column] != DBNull.Value)
									property.SetValue(obj, Convert.ChangeType(row[column], property.PropertyType));
							}
							results.Add(obj);
						}
					}
					return results;
				}
			}
		}

		public object[] CreateParameters<T>(T item)
		{
			var properties = typeof(T).GetProperties();
			return properties
				.Select(p => new SqlParameter($"@{p.Name}", p.GetValue(item) ?? DBNull.Value))
				.ToArray();
		}

		public async Task<object?> ExecuteScalarAsync(string query, object[]? parameters)
		{
			await OpenConnectionAsync();

			using (SqlCommand sqlCommand = new SqlCommand(query, _connection))
			{
				if (parameters != null)
					sqlCommand.Parameters.AddRange(parameters);

				return await sqlCommand.ExecuteScalarAsync();
			}
		}

		public async Task<int> ExecuteNonQueryAsync(string query, object[]? parameters = null)
		{
			await OpenConnectionAsync();

			using (SqlCommand sqlCommand = new SqlCommand(query, _connection))
			{
				if (parameters != null)
					sqlCommand.Parameters.AddRange(parameters);

				return await sqlCommand.ExecuteNonQueryAsync();
			}
		}

		public async Task<int> ExecuteNonQueryAsync(string query, object[]? parameters, IDbTransaction? transaction)
		{
			await OpenConnectionAsync();

			using (SqlCommand sqlCommand = new SqlCommand(query, _connection, (SqlTransaction?)transaction))
			{
				if (parameters != null)
					sqlCommand.Parameters.AddRange(parameters);

				return await sqlCommand.ExecuteNonQueryAsync();
			}
		}

		public async Task<bool> ExecuteTransactionAsync(params Func<IDbTransaction, Task<int>>[] operations)
		{
			await OpenConnectionAsync();

			using (SqlTransaction transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				try
				{
					foreach (var operation in operations)
					{
						int result = await operation(transaction);
						if (result == 0)
						{
							transaction.Rollback();
							return false;
						}
					}

					transaction.Commit();
					return true;
				}
				catch
				{
					transaction.Rollback();
					throw;
				}
			}
		}

		public async Task<bool> Insert<T>(T item, string insertQuery) where T : new()
		{
			return await ExecuteNonQueryAsync(insertQuery, CreateParameters(item)) > 0;
		}

		public async Task<bool> Update<T>(T item, string updateQuery) where T : new()
		{
			return await ExecuteNonQueryAsync(updateQuery, CreateParameters(item)) > 0;
		}

		public async Task<bool> Delete<T>(T item, string deleteQuery) where T : new()
		{
			return await ExecuteNonQueryAsync(deleteQuery, CreateParameters(item)) > 0;
		}

	}
}
