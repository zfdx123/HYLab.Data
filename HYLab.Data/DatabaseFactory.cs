using HYLab.Data.DBase;
using HYLab.Data.MySql;
using HYLab.Data.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HYLab.Data
{
	public static class DatabaseFactory
	{
		private static readonly object _lock = new object();
		private static BaseDatabase? _instance;

		public static BaseDatabase GetInstance()
		{
			if (_instance == null)
			{
				throw new NullReferenceException("未初始化");
			}
			return _instance;
		}

		public static BaseDatabase GetInstance(ServerType type, string connectionString)
		{
			if (_instance == null)
			{
				lock (_lock)
				{
					if (_instance == null)
					{
						_instance = type switch
						{
							ServerType.SqlServer => new SqlServerDatabase(connectionString),
							ServerType.MySql => new MySqlDatabase(connectionString),
							// 支持更多数据库
							_ => throw new NotSupportedException($"Unsupported database type: {type}")
						};
					}
				}
			}
			return _instance;
		}
	}

}
