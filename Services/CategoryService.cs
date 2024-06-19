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

        public async Task<(Category, string)> AddAsync(CategoryDto request)
        {
            var existingCategory = await _categoryRepository.GetByNameAsync(request.Name);
            if (existingCategory != null)
            {
                return (null, "Category already exists.");
            }

            var category = new Category { Name = request.Name };
            var addedCategory = await _categoryRepository.AddAsync(category);
            return (addedCategory, null);
        }

        public async Task<(Category, string)> UpdateAsync(int id, CategoryDto request)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return (null, "Category not found.");
            }

            var existingCategory = await _categoryRepository.GetByNameAsync(request.Name);
            if (existingCategory != null && existingCategory.CategoryId != id)
            {
                return (null, "Category already exists.");
            }

            category.Name = request.Name;
            var updatedCategory = await _categoryRepository.UpdateAsync(category);
            return (updatedCategory, null);
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category != null)
            {
                await _categoryRepository.DeleteAsync(id);
            }
        }
    }
}
