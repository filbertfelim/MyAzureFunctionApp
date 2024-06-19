using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;

namespace MyAzureFunctionApp.Services
{
    public interface IAuthorService : IService<Author>
    {
        Task<Author> AddAsync(AuthorDto request);
        Task<Author> UpdateAsync(int id, AuthorDto request);
        Task<Author> GetByIdAsync(int id);
        Task DeleteAsync(int id);
    }
}
