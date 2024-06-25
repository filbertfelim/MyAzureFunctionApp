using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class Service<T> : IService<T> where T : class
    {
        private readonly ICrudRepository<T> _repository;

        public Service(ICrudRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }
    }
}
