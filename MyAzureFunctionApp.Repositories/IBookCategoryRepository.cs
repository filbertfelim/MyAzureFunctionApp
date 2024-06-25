using MyAzureFunctionApp.Models;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface IBookCategoryRepository
    {
        Task<IEnumerable<BookCategory>> GetByBookIdAsync(int bookId);
        Task<BookCategory> AddAsync(BookCategory bookCategory);
        Task DeleteByBookIdAsync(int bookId);
        Task DeleteByCategoryIdAsync(int categoryId);
    }
}
