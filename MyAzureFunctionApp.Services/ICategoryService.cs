using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;

namespace MyAzureFunctionApp.Services
{
    public interface ICategoryService : IService<Category>
    {
        Task<(Category, string)> AddAsync(CategoryDto request);
        Task<(Category, string)> UpdateAsync(int id, CategoryDto request);
        Task<Category> GetByIdAsync(int id);
        Task DeleteAsync(int id);
    }
}
