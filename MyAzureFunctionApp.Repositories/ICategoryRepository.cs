using MyAzureFunctionApp.Models;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface ICategoryRepository : ICrudRepository<Category>
    {
        Task<Category> GetByNameAsync(string name);
    }
}
