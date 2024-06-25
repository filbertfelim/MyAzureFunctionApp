using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> CommitAsync();
    }
}