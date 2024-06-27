using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Xunit;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Tests
{
    public class DatabaseSchemaTests
    {
        private readonly string _connectionString;
        
        public DatabaseSchemaTests()
        {
            _connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") 
                ?? throw new InvalidOperationException("Connection string not found in environment variables.");
        }

        [Fact]
        public async Task Models_ShouldMatchDatabaseSchema()
        {
            var currentSchema = await GetCurrentDatabaseSchemaAsync();

            AssertModelsMatchSchema(currentSchema, typeof(Category));
            AssertModelsMatchSchema(currentSchema, typeof(Book));
            AssertModelsMatchSchema(currentSchema, typeof(Author));
            AssertModelsMatchSchema(currentSchema, typeof(BookCategory));
        }

        private async Task<string[]> GetCurrentDatabaseSchemaAsync()
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                var query = @"
                    SELECT table_name, column_name
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                    ORDER BY table_name, ordinal_position;";

                var schemaInfo = await db.QueryAsync<SchemaInfo>(query);

                return schemaInfo.Select(si => $"{si.TABLE_NAME}.{si.COLUMN_NAME}").ToArray();
            }
        }

        private void AssertModelsMatchSchema(string[] currentSchema, Type modelType)
        {
            var modelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => IsColumn(p))
                                        .Select(p => $"{modelType.Name}.{p.Name}");
            foreach (var property in modelProperties)
            {
                Assert.Contains(property, currentSchema);
            }
        }
        private bool IsColumn(PropertyInfo propertyInfo)
        {
            var propertyType = propertyInfo.PropertyType;
            return propertyType == typeof(string) || propertyType == typeof(int);
        }
        private class SchemaInfo
        {
            public string TABLE_NAME { get; set; }
            public string COLUMN_NAME { get; set; }
        }
    }
}