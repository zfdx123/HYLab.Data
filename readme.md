# MySqlData

基础数据库框架，支持 MySQL 和 SQL Server 等数据库操作。

## 功能
- 支持多种数据库类型
- 高效的数据库连接管理
- 灵活的配置

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HYLab.Data
using HYLab.Data.DBase;

class Program
{
    static async Task Main(string[] args)
    {
        string connectionString = "Your_Connection_String_Here";
        BaseDatabase db = DatabaseFactory.GetInstance("MySql||sqlserver",connectionString);
        // 单例模式 只获取对象
        // BaseDatabase db = DatabaseFactory.GetInstance();

        try
        {
            // 插入数据
            var newPerson = new Person
            {
                Name = "Alice",
                Age = 30,
                BirthDate = new DateTime(1993, 5, 1)
            };
            string insertQuery = "INSERT INTO Person (Name, Age, BirthDate) VALUES (@Name, @Age, @BirthDate)";
            bool insertResult = await db.Insert(newPerson, insertQuery);
            Console.WriteLine($"Insert successful: {insertResult}");

            // 查询数据
            string selectQuery = "SELECT Id, Name, Age, BirthDate FROM Person";
            List<Person> persons = await db.Select<Person>(selectQuery, null);
            foreach (var person in persons)
            {
                Console.WriteLine($"Id: {person.Id}, Name: {person.Name}, Age: {person.Age}, BirthDate: {person.BirthDate}");
            }

            // 更新数据
            var updatePerson = persons[0];
            updatePerson.Age = 31;
            string updateQuery = "UPDATE Person SET Name = @Name, Age = @Age, BirthDate = @BirthDate WHERE Id = @Id";
            bool updateResult = await db.Update(updatePerson, updateQuery);
            Console.WriteLine($"Update successful: {updateResult}");

            // 删除数据
            var deletePerson = persons[0];
            string deleteQuery = "DELETE FROM Person WHERE Id = @Id";
            bool deleteResult = await db.Delete(deletePerson, deleteQuery);
            Console.WriteLine($"Delete successful: {deleteResult}");

            // 事务
            bool transactionResult = await db.ExecuteTransactionAsync(
                async (transaction) =>
                {
                    // Step 1: Insert into Person table
                    string personInsertQuery = "INSERT INTO Person (Name, Age) VALUES (@Name, @Age)";
                    var personParams = new[]
                    {
                        new SqlParameter("@Name", "John Doe"),
                        new SqlParameter("@Age", 35)
                    };
                    int personInsertResult = await db.ExecuteNonQueryAsync(personInsertQuery, personParams, transaction);

                    // Simulate failure (uncomment the next line to test rollback)
                    // throw new Exception("Simulating failure after Person insert!");

                    return personInsertResult;
                },
                async (transaction) =>
                {
                    // Step 2: Get the last inserted Person ID
                    string personIdQuery = "SELECT SCOPE_IDENTITY()";
                    object personIdResult = await db.ExecuteScalar("SELECT SCOPE_IDENTITY()", null);
                    int personId = Convert.ToInt32(personIdResult);

                    // Step 3: Insert into Address table
                    string addressInsertQuery = "INSERT INTO Address (PersonId, AddressLine) VALUES (@PersonId, @AddressLine)";
                    var addressParams = new[]
                    {
                        new SqlParameter("@PersonId", personId),
                        new SqlParameter("@AddressLine", "123 Main Street")
                    };
                    return await db.ExecuteNonQueryAsync(addressInsertQuery, addressParams, transaction);
                }
            );

            Console.WriteLine($"Transaction completed successfully: {transactionResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            db.Close();
        }
    }
}

```