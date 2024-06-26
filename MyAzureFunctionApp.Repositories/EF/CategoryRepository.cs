using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyAzureFunctionApp.Models;

namespace MyAzureFunctionApp.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Category.Include(c => c.BookCategories)
                                            .ThenInclude(bc => bc.Book)
                                            .ToListAsync();
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            return await _context.Category.Include(c => c.BookCategories)
                                            .ThenInclude(bc => bc.Book)
                                            .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<Category> GetByNameAsync(string name)
        {
            return await _context.Category.FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<Category> AddAsync(Category category)
        {
            _context.Category.Add(category);
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _context.Category.Update(category);
            return category;
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category != null)
            {
                _context.Category.Remove(category);
            }
        }
    }
}
