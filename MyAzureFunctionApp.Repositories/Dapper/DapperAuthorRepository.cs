using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories.Dapper
{
    public class DapperAuthorRepository : DapperBaseRepository, IAuthorRepository
    {
        public DapperAuthorRepository(IDbConnection connection, int commandTimeout) 
            : base(connection, commandTimeout)
        {
        }

        public async Task<IEnumerable<Author>> GetAllAsync()
        {
            var sql = SqlQueries.GetQuery("GetAllAuthors");

            return await WithRetryPolicy(async () =>
            {
                var authorDictionary = new Dictionary<int, Author>();

                var result = await _connection.QueryAsync<Author, Book, Author>(
                    CreateCommand(sql),
                    (author, book) =>
                    {
                        if (!authorDictionary.TryGetValue(author.AuthorId, out var currentAuthor))
                        {
                            currentAuthor = author;
                            currentAuthor.Books = new List<Book>();
                            authorDictionary.Add(currentAuthor.AuthorId, currentAuthor);
                        }

                        if (book != null)
                        {
                            book.AuthorId = author.AuthorId;
                            currentAuthor.Books.Add(book);
                        }

                        return currentAuthor;
                    },
                    splitOn: "BookId");

                return authorDictionary.Values;
            });
        }

        public async Task<Author> GetByIdAsync(int id)
        {
            var sql = SqlQueries.GetQuery("GetAuthorById");

            return await WithRetryPolicy(async () =>
            {
                var authorDictionary = new Dictionary<int, Author>();

                var result = await _connection.QueryAsync<Author, Book, Author>(
                    CreateCommand(sql, new { Id = id }),
                    (author, book) =>
                    {
                        if (!authorDictionary.TryGetValue(author.AuthorId, out var currentAuthor))
                        {
                            currentAuthor = author;
                            currentAuthor.Books = new List<Book>();
                            authorDictionary.Add(currentAuthor.AuthorId, currentAuthor);
                        }

                        if (book != null)
                        {
                            book.AuthorId = author.AuthorId;
                            currentAuthor.Books.Add(book);
                        }

                        return currentAuthor;
                    },
                    splitOn: "BookId");

                return authorDictionary.Values.FirstOrDefault();
            });
        }

        public async Task<Author> AddAsync(Author author)
        {
            var sql = SqlQueries.GetQuery("AddAuthor");

            return await WithRetryPolicy(async () =>
            {
                return await _connection.QuerySingleAsync<Author>(CreateCommand(sql, author));
            });
        }

        public async Task<Author> UpdateAsync(Author author)
        {
            var updateSql = SqlQueries.GetQuery("UpdateAuthor");
            var fetchSql = SqlQueries.GetQuery("GetAuthorById");

            return await WithRetryPolicy(async () =>
            {
                // Update the author
                await _connection.ExecuteAsync(CreateCommand(updateSql, author));

                // Fetch the updated author with books
                var authorDictionary = new Dictionary<int, Author>();

                var result = await _connection.QueryAsync<Author, Book, Author>(
                    CreateCommand(fetchSql, new { Id = author.AuthorId }),
                    (updatedAuthor, book) =>
                    {
                        if (!authorDictionary.TryGetValue(updatedAuthor.AuthorId, out var currentAuthor))
                        {
                            currentAuthor = updatedAuthor;
                            currentAuthor.Books = new List<Book>();
                            authorDictionary.Add(currentAuthor.AuthorId, currentAuthor);
                        }

                        if (book != null)
                        {
                            book.AuthorId = updatedAuthor.AuthorId;
                            currentAuthor.Books.Add(book);
                        }

                        return currentAuthor;
                    },
                    splitOn: "BookId");

                return authorDictionary.Values.FirstOrDefault();
            });
        }

        public async Task DeleteAsync(int id)
        {
            var sql = SqlQueries.GetQuery("DeleteAuthor");

            await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(CreateCommand(sql, new { Id = id }));
            });
        }
    }
}
