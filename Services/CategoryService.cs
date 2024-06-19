using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class CategoryService : Service<Category>, ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository) : base(categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Category> AddAsync(CategoryDto request)
        {
            var category = new Category { Name = request.Name };
            return await _categoryRepository.AddAsync(category);
        }

        public async Task<Category> UpdateAsync(int id, CategoryDto request)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return null;
            }

            category.Name = request.Name;
            return await _categoryRepository.UpdateAsync(category);
        }
    }
}
