using MyAzureFunctionApp.Models;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface IBookRepository : IRepository<Book>
    {
        Task<Book> AddAsync(Book book);
        Task<Book> UpdateAsync(Book book);
    }
}
