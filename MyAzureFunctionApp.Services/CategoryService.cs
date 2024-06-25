using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class CategoryService : Service<Category>, ICategoryService
    {
        private readonly IDapperUnitOfWork _unitOfWork;

        public CategoryService(IDapperUnitOfWork unitOfWork) : base(unitOfWork.Categories)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(Category, string)> AddAsync(CategoryDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCategory = await _unitOfWork.Categories.GetByNameAsync(request.Name);
                if (existingCategory != null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (null, "Category already exists.");
                }

                var category = new Category { Name = request.Name };
                var createdCategory = await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.CommitAsync();
                return (createdCategory, null);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<(Category, string)> UpdateAsync(int id, CategoryDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return (null, "Category not found.");
                }

                var existingCategory = await _unitOfWork.Categories.GetByNameAsync(request.Name);
                if (existingCategory != null && existingCategory.CategoryId != id)
                {
                    await _unitOfWork.RollbackAsync();
                    return (null, "Category already exists.");
                }

                category.Name = request.Name;
                await _unitOfWork.Categories.UpdateAsync(category);
                await _unitOfWork.CommitAsync();
                return (category, null);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            return await _unitOfWork.Categories.GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category != null)
                {
                    await _unitOfWork.BookCategories.DeleteByCategoryIdAsync(id);
                    await _unitOfWork.Categories.DeleteAsync(id);
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
