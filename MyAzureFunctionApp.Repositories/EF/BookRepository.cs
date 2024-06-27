using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly AppDbContext _context;

        public BookRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateImagePathAsync(int bookId, string imagePath)
        {
            //
        }

        public async Task<IEnumerable<Book>> GetAllAsync()
        {
            return await _context.Book.Include(b => b.Author)
                                       .Include(b => b.BookCategories)
                                       .ThenInclude(bc => bc.Category)
                                       .ToListAsync();
        }

        public async Task<Book> GetByIdAsync(int id)
        {
            return await _context.Book.Include(b => b.Author)
                                       .Include(b => b.BookCategories)
                                       .ThenInclude(bc => bc.Category)
                                       .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<Book> AddAsync(Book book)
        {
            _context.Book.Add(book);
            return book;
        }

        public async Task<Book> UpdateAsync(Book book)
        {
            _context.Book.Update(book);
            return book;
        }

        public async Task DeleteAsync(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book != null)
            {
                _context.Book.Remove(book);
            }
        }
    }
}
