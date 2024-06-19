using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class BookService : Service<Book>, IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly ICategoryRepository _categoryRepository;

        public BookService(IBookRepository bookRepository, IAuthorRepository authorRepository, ICategoryRepository categoryRepository) : base(bookRepository)
        {
            _bookRepository = bookRepository;
            _authorRepository = authorRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<(Book, string)> AddAsync(BookDto request)
        {
            var author = await _authorRepository.GetByIdAsync(request.AuthorId);
            if (author == null)
            {
                return (null, "Author not found.");
            }

            var categories = new List<BookCategory>();
            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _categoryRepository.GetByIdAsync(categoryId);
                if (category == null)
                {
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

            var createdBook = await _bookRepository.AddAsync(book);
            return (createdBook, null);
        }

        public async Task<(Book, string)> UpdateAsync(int id, BookDto request)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
            {
                return (null, "Book not found.");
            }

            var author = await _authorRepository.GetByIdAsync(request.AuthorId);
            if (author == null)
            {
                return (null, "Author not found.");
            }

            var categories = new List<BookCategory>();
            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _categoryRepository.GetByIdAsync(categoryId);
                if (category == null)
                {
                    return (null, $"Category with ID {categoryId} not found.");
                }
                categories.Add(new BookCategory { CategoryId = categoryId });
            }

            book.Title = request.Title;
            book.AuthorId = request.AuthorId;
            book.BookCategories = categories;

            var updatedBook = await _bookRepository.UpdateAsync(book);
            return (updatedBook, null);
        }

        public async Task<Book> GetByIdAsync(int id)
        {
            return await _bookRepository.GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book != null)
            {
                await _bookRepository.DeleteAsync(id);
            }
        }
    }
}
