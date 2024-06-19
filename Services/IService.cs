using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Services
{
    public interface IService<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        
    }
}
