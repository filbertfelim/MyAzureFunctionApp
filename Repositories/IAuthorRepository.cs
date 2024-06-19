using MyAzureFunctionApp.Models;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface IAuthorRepository : IRepository<Author>
    {
        Task<Author> AddAsync(Author author);
        Task<Author> UpdateAsync(Author author);
    }
}
