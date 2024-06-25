using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories.Dapper
{
    public class DapperBookRepository : DapperBaseRepository, IBookRepository
    {
        public DapperBookRepository(IDbConnection connection, int commandTimeout) 
            : base(connection, commandTimeout)
        {
        }

        public async Task<IEnumerable<Book>> GetAllAsync()
        {
            var sql = SqlQueries.GetQuery("GetAllBooks");

            return await WithRetryPolicy(async () =>
            {
                var bookDictionary = new Dictionary<int, Book>();

                var result = await _connection.QueryAsync<Book, Author, BookCategory, Category, Book>(
                    sql,
                    (book, author, bookCategory, category) =>
                    {
                        // Check if the book already exists in the dictionary
                        if (!bookDictionary.TryGetValue(book.BookId, out var currentBook))
                        {
                            currentBook = book;
                            currentBook.Author = author;
                            currentBook.BookCategories = new List<BookCategory>();
                            bookDictionary.Add(currentBook.BookId, currentBook);
                        }

                        // Add the BookCategory and Category only if they are not null
                        if (bookCategory != null && category != null)
                        {
                            bookCategory.BookId = currentBook.BookId;
                            bookCategory.CategoryId = category.CategoryId;
                            bookCategory.Book = currentBook;
                            bookCategory.Category = category;
                            currentBook.BookCategories.Add(bookCategory);
                        }

                        return currentBook;
                    },
                    splitOn: "AuthorId,BookId,CategoryId");

                return bookDictionary.Values;
            });
        }


        public async Task<Book> GetByIdAsync(int id)
        {
            var sql = SqlQueries.GetQuery("GetBookById");

            return await WithRetryPolicy(async () =>
            {
                var bookDictionary = new Dictionary<int, Book>();

                var result = await _connection.QueryAsync<Book, Author, BookCategory, Category, Book>(
                    CreateCommand(sql, new { Id = id }),
                    (book, author, bookCategory, category) =>
                    {
                        if (!bookDictionary.TryGetValue(book.BookId, out var currentBook))
                        {
                            currentBook = book;
                            currentBook.Author = author;
                            currentBook.BookCategories = new List<BookCategory>();
                            bookDictionary.Add(currentBook.BookId, currentBook);
                        }

                        if (bookCategory != null && category != null)
                        {
                            bookCategory.BookId = currentBook.BookId;
                            bookCategory.CategoryId = category.CategoryId;
                            bookCategory.Book = currentBook;
                            bookCategory.Category = category;
                            currentBook.BookCategories.Add(bookCategory);
                        }

                        return currentBook;
                    },
                    splitOn: "AuthorId,BookId,CategoryId");

                return bookDictionary.Values.FirstOrDefault();
            });
        }

        public async Task<Book> AddAsync(Book book)
        {
            var sql = SqlQueries.GetQuery("AddBook");

            return await WithRetryPolicy(async () =>
            {
                var bookId = await _connection.ExecuteScalarAsync<int>(CreateCommand(sql, new { book.Title, book.AuthorId }));
                return await GetByIdAsync(bookId);
            });
        }

        public async Task<Book> UpdateAsync(Book book)
        {
            var updateSql = SqlQueries.GetQuery("UpdateBook");
            var fetchSql = SqlQueries.GetQuery("GetBookById");

            return await WithRetryPolicy(async () =>
            {
                // Update the book
                await _connection.ExecuteAsync(CreateCommand(updateSql, new { book.Title, book.AuthorId, book.BookId }));

                // Fetch the updated book with author and categories
                var bookDictionary = new Dictionary<int, Book>();

                var result = await _connection.QueryAsync<Book, Author, BookCategory, Category, Book>(
                    CreateCommand(fetchSql, new { Id = book.BookId }),
                    (updatedBook, author, bookCategory, category) =>
                    {
                        if (!bookDictionary.TryGetValue(updatedBook.BookId, out var currentBook))
                        {
                            currentBook = updatedBook;
                            currentBook.Author = author;
                            currentBook.BookCategories = new List<BookCategory>();
                            bookDictionary.Add(currentBook.BookId, currentBook);
                        }

                        if (bookCategory != null && category != null)
                        {
                            bookCategory.Book = currentBook;
                            bookCategory.Category = category;
                            currentBook.BookCategories.Add(bookCategory);
                        }

                        return currentBook;
                    },
                    splitOn: "AuthorId,CategoryId,CategoryId");

                return bookDictionary.Values.FirstOrDefault();
            });
        }

        public async Task DeleteAsync(int id)
        {
            var sql = SqlQueries.GetQuery("DeleteBook");

            await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(CreateCommand(sql, new { Id = id }));
            });
        }

        public async Task UpdateImagePathAsync(int bookId, string imagePath)
        {
            var sql = SqlQueries.GetQuery("UpdateBookImagePath");

            await WithRetryPolicy(async () =>
            {
                await _connection.ExecuteAsync(CreateCommand(sql, new { BookId = bookId, ImagePath = imagePath }));
            });
        }
    }
}
