using System.Data;
using MyAzureFunctionApp.Repositories.Dapper;
using Npgsql;
using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public class DapperUnitOfWork : IDapperUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;
        private bool _disposed;

        public IBookRepository Books { get; }
        public IAuthorRepository Authors { get; }
        public ICategoryRepository Categories { get; }
        public IBookCategoryRepository BookCategories { get; }

        public DapperUnitOfWork(IDbConnection connection, int commandTimeout)
        {
            _connection = connection;
            _connection.Open();
            Books = new DapperBookRepository(_connection, commandTimeout);
            Authors = new DapperAuthorRepository(_connection, commandTimeout);
            Categories = new DapperCategoryRepository(_connection, commandTimeout);
            BookCategories = new DapperBookCategoryRepository(_connection, commandTimeout);
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = _connection.BeginTransaction();
            ((DapperBaseRepository)Books).SetTransaction(_transaction);
            ((DapperBaseRepository)Authors).SetTransaction(_transaction);
            ((DapperBaseRepository)Categories).SetTransaction(_transaction);
            ((DapperBaseRepository)BookCategories).SetTransaction(_transaction);
            await Task.CompletedTask;
        }

        public async Task CommitAsync()
        {
            try
            {
                _transaction?.Commit();
                _transaction = null;
                await Task.CompletedTask; // to make it async
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            _transaction?.Rollback();
            _transaction = null;
            await Task.CompletedTask; // to make it async
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection.Dispose();
                _disposed = true;
            }
        }
    }
}
