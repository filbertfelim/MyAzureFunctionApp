using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace MyAzureFunctionApp.Services
{
    public interface IBookService : IService<Book>
    {
        Task<(Book, string)> AddAsync(BookDto request);
        Task<(Book, string)> UpdateAsync(int id, BookDto request);
        Task<Book> GetByIdAsync(int id);
        Task DeleteAsync(int id);
        Task<string> UploadBookImageAsync(int bookId, IFormFile imageFile);
    }
}
