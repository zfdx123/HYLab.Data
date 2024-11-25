using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using HYLab.Data.DBase;

namespace HYLab.Data.MySql
{
	public class MySqlDatabase : BaseDatabase
	{
		private readonly MySqlConnection _connection;

		public MySqlDatabase(string connectionString)
		{
			_connection = new MySqlConnection(connectionString);
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

			using (MySqlCommand sqlCommand = new MySqlCommand(query, _connection))
			{
				if (parameters != null)
					sqlCommand.Parameters.AddRange(parameters);

				using (MySqlDataAdapter adapter = new MySqlDataAdapter(sqlCommand))
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
								{
									var value = row[column];
									if (property.PropertyType == typeof(DateTime?))
									{
										property.SetValue(obj, (DateTime?)value);
									}
									else
									{
										property.SetValue(obj, Convert.ChangeType(value, property.PropertyType));
									}
								}
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
				.Select(p => new MySqlParameter($"@{p.Name}", p.GetValue(item) ?? DBNull.Value))
				.ToArray();
		}

		public async Task<object?> ExecuteScalarAsync(string query, object[]? parameters)
		{
			await OpenConnectionAsync();

			using (var command = new MySqlCommand(query, _connection))
			{
				if (parameters != null)
					command.Parameters.AddRange(parameters);

				return await command.ExecuteScalarAsync();
			}
		}

		public async Task<int> ExecuteNonQueryAsync(string query, object[]? parameters = null)
		{
			await OpenConnectionAsync();

			using (var command = new MySqlCommand(query, _connection))
			{
				if (parameters != null)
					command.Parameters.AddRange(parameters);

				return await command.ExecuteNonQueryAsync();
			}
		}

		public async Task<int> ExecuteNonQueryAsync(string query, object[]? parameters, IDbTransaction? transaction)
		{
			await OpenConnectionAsync();

			using (var command = new MySqlCommand(query, _connection))
			{
				if (transaction != null)
					command.Transaction = (MySqlTransaction)transaction;

				if (parameters != null)
					command.Parameters.AddRange(parameters);

				return await command.ExecuteNonQueryAsync();
			}
		}


		public async Task<bool> ExecuteTransactionAsync(params Func<IDbTransaction, Task<int>>[] operations)
		{
			await OpenConnectionAsync();

			using (var transaction = _connection.BeginTransaction())
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
