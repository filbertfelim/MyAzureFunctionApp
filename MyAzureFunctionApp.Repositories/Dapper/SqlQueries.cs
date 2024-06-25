using System;
using System.Globalization;
using System.Resources;

namespace MyAzureFunctionApp.Repositories.Dapper
{
    public static class SqlQueries
    {
        private static readonly ResourceManager ResourceManager;

        static SqlQueries()
        {
            ResourceManager = new ResourceManager("MyAzureFunctionApp.Repositories.SqlQueries", typeof(SqlQueries).Assembly);
        }

        public static string GetQuery(string key)
        {
            var query = ResourceManager.GetString(key, CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(query))
            {
                throw new KeyNotFoundException($"SQL query for key '{key}' not found.");
            }

            return query;
        }
    }
}
