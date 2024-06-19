using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class Service<T> : IService<T> where T : class
    {
        private readonly IRepository<T> _repository;

        public Service(IRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }
    }
}
