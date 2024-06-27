using Microsoft.EntityFrameworkCore;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories
{
    public class AuthorRepository : IAuthorRepository
    {
        private readonly AppDbContext _context;

        public AuthorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Author>> GetAllAsync()
        {
            return await _context.Author.Include(a => a.Books).ToListAsync();
        }

        public async Task<Author> GetByIdAsync(int id)
        {
            return await _context.Author.Include(a => a.Books)
                                         .FirstOrDefaultAsync(a => a.AuthorId == id);
        }

        public async Task<Author> AddAsync(Author author)
        {
            _context.Author.Add(author);
            return author;
        }

        public async Task<Author> UpdateAsync(Author author)
        {
            _context.Author.Update(author);
            return author;
        }

        public async Task DeleteAsync(int id)
        {
            var author = await _context.Author.FindAsync(id);
            if (author != null)
            {
                _context.Author.Remove(author);
            }
        }
    }
}
