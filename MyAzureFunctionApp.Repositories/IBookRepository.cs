using MyAzureFunctionApp.Models;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface IBookRepository : ICrudRepository<Book>
    {
        Task UpdateImagePathAsync(int bookId, string imagePath);
    }
}
