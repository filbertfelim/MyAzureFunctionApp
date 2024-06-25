using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories.Dapper
{
    public class DapperCategoryRepository : DapperBaseRepository, ICategoryRepository
    {
        public DapperCategoryRepository(IDbConnection connection, int commandTimeout) 
            : base(connection, commandTimeout)
        {
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            var sql = SqlQueries.GetQuery("GetAllCategories");

            return await WithRetryPolicy(async () =>
            {
                var result = await _connection.QueryAsync<Category>(
                    CreateCommand(sql)
                );

                return result;
            });
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            var sql = SqlQueries.GetQuery("GetCategoryById");

            return await WithRetryPolicy(async () =>
            {
                var result = await _connection.QuerySingleOrDefaultAsync<Category>(
                    CreateCommand(sql, new { Id = id })
                );

                return result;
            });
        }

        public async Task<Category> GetByNameAsync(string name)
        {
            var sql = SqlQueries.GetQuery("GetCategoryByName");

            return await WithRetryPolicy(async () =>
            {
                var result = await _connection.QuerySingleOrDefaultAsync<Category>(
                    CreateCommand(sql, new { Name = name })
                );

                return result;
            });
        }

        public async Task<Category> AddAsync(Category category)
        {
            var sql = SqlQueries.GetQuery("AddCategory");

            return await WithRetryPolicy(async () =>
            {
                var categoryId = await _connection.ExecuteScalarAsync<int>(
                    CreateCommand(sql, new { category.Name })
                );
                category.CategoryId = categoryId;
                return category;
            });
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            var sql = SqlQueries.GetQuery("UpdateCategory");

            return await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(
                    CreateCommand(sql, new { category.Name, category.CategoryId })
                );

                return category;
            });
        }

        public async Task DeleteAsync(int id)
        {
            var sql = SqlQueries.GetQuery("DeleteCategory");

            await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(
                    CreateCommand(sql, new { Id = id })
                );
            });
        }
    }
}
