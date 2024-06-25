using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface ICrudRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task DeleteAsync(int id);
        Task<T> AddAsync(T model);
        Task<T> UpdateAsync(T model);
    }
}
