using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class BookService : Service<Book>, IBookService
    {
        private readonly IDapperUnitOfWork _unitOfWork;

        public BookService(IDapperUnitOfWork unitOfWork) : base(unitOfWork.Books)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(Book, string)> AddAsync(BookDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var author = await _unitOfWork.Authors.GetByIdAsync(request.AuthorId);
                if (author == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (null, "Author not found.");
                }

                var categories = new List<BookCategory>();
                foreach (var categoryId in request.CategoryIds)
                {
                    var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
                    if (category == null)
                    {
                        await _unitOfWork.RollbackAsync();
                        return (null, $"Category with ID {categoryId} not found.");
                    }
                    categories.Add(new BookCategory { CategoryId = categoryId });
                }

                var book = new Book
                {
                    Title = request.Title,
                    AuthorId = request.AuthorId,
                    BookCategories = categories
                };

                var addedBook = await _unitOfWork.Books.AddAsync(book);

                // Add BookCategory entries
                foreach (var bookCategory in categories)
                {
                    bookCategory.BookId = addedBook.BookId;
                    await _unitOfWork.BookCategories.AddAsync(bookCategory);
                }

                await _unitOfWork.CommitAsync();        
                return (book, null);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<(Book, string)> UpdateAsync(int id, BookDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var book = await _unitOfWork.Books.GetByIdAsync(id);
                if (book == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (null, "Book not found.");
                }

                var author = await _unitOfWork.Authors.GetByIdAsync(request.AuthorId);
                if (author == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (null, "Author not found.");
                }

                var categories = new List<BookCategory>();
                foreach (var categoryId in request.CategoryIds)
                {
                    var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
                    if (category == null)
                    {
                        await _unitOfWork.RollbackAsync();
                        return (null, $"Category with ID {categoryId} not found.");
                    }
                    categories.Add(new BookCategory { CategoryId = categoryId });
                }

                book.Title = request.Title;
                book.AuthorId = request.AuthorId;

                await _unitOfWork.BookCategories.DeleteByBookIdAsync(book.BookId);

                await _unitOfWork.Books.UpdateAsync(book);
                foreach (var bookCategory in categories)
                {
                    bookCategory.BookId = book.BookId;
                    await _unitOfWork.BookCategories.AddAsync(bookCategory);
                }

                await _unitOfWork.Books.UpdateAsync(book);
                await _unitOfWork.CommitAsync();
                return (book, null);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<Book> GetByIdAsync(int id)
        {
            return await _unitOfWork.Books.GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var book = await _unitOfWork.Books.GetByIdAsync(id);
                if (book != null)
                {
                    await _unitOfWork.BookCategories.DeleteByBookIdAsync(book.BookId);
                    await _unitOfWork.Books.DeleteAsync(id);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    await _unitOfWork.RollbackAsync();
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
