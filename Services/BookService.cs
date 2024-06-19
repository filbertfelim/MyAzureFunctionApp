using System.Linq;
using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class BookService : Service<Book>, IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository) : base(bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<Book> AddAsync(BookDto request)
        {
            var book = new Book
            {
                Title = request.Title,
                AuthorId = request.AuthorId,
                BookCategories = request.CategoryIds.Select(id => new BookCategory { CategoryId = id }).ToList()
            };
            return await _bookRepository.AddAsync(book);
        }

        public async Task<Book> UpdateAsync(int id, BookDto request)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
            {
                return null;
            }

            book.Title = request.Title;
            book.AuthorId = request.AuthorId;
            book.BookCategories = request.CategoryIds.Select(id => new BookCategory { CategoryId = id }).ToList();
            return await _bookRepository.UpdateAsync(book);
        }
    }
}
