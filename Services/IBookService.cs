using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;

namespace MyAzureFunctionApp.Services
{
    public interface IBookService : IService<Book>
    {
        Task<Book> AddAsync(BookDto request);
        Task<Book> UpdateAsync(int id, BookDto request);
        Task<Book> GetByIdAsync(int id);
        Task DeleteAsync(int id);
    }
}
