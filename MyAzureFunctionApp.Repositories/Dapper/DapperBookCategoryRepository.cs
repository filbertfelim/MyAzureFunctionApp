using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories.Dapper
{
    public class DapperBookCategoryRepository : DapperBaseRepository, IBookCategoryRepository
    {
        public DapperBookCategoryRepository(IDbConnection connection, int commandTimeout)
            : base(connection, commandTimeout)
        {
        }

        public async Task<IEnumerable<BookCategory>> GetByBookIdAsync(int bookId)
        {
            var sql = SqlQueries.GetQuery("GetBookCategoriesByBookId");

            return await WithRetryPolicy(async () =>
            {
                return await _connection.QueryAsync<BookCategory>(CreateCommand(sql, new { BookId = bookId }));
            });
        }

        public async Task<BookCategory> GetByIdAsync(int bookId, int categoryId)
        {
            var sql = SqlQueries.GetQuery("GetBookCategoriesById");

            return await WithRetryPolicy(async () =>
            {
                return await _connection.QuerySingleOrDefaultAsync<BookCategory>(CreateCommand(sql, new { BookId = bookId, CategoryId = categoryId }));
            });
        }

        public async Task<BookCategory> AddAsync(BookCategory bookCategory)
        {
            var sql = SqlQueries.GetQuery("AddBookCategory");

            return await WithRetryPolicy(async () =>
            {
                var id = await _connection.ExecuteScalarAsync<int>(CreateCommand(sql, bookCategory));
                return await GetByIdAsync(bookCategory.BookId, bookCategory.CategoryId);
            });
        }

        public async Task DeleteByBookIdAsync(int bookId)
        {
            var sql = SqlQueries.GetQuery("DeleteBookCategoriesByBookId");

            await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(CreateCommand(sql, new { BookId = bookId }));
            });
        }

        public async Task DeleteByCategoryIdAsync(int categoryId)
        {
            var sql = SqlQueries.GetQuery("DeleteBookCategoriesByCategoryId");

            await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(CreateCommand(sql, new { CategoryId = categoryId }));
            });
        }
    }
}
