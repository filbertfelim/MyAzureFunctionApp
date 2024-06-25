using System.Data;
using Dapper;
using Polly;
using Polly.Retry;

namespace MyAzureFunctionApp.Repositories.Dapper
{
    public abstract class DapperBaseRepository
    {
        protected readonly IDbConnection _connection;
        private readonly int _commandTimeout;
        private readonly AsyncRetryPolicy _retryPolicy;
        private IDbTransaction _transaction;

        protected DapperBaseRepository(IDbConnection connection, int commandTimeout)
        {
            _connection = connection;
            _commandTimeout = commandTimeout;
            _retryPolicy = Policy.Handle<Exception>()
                                 .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        protected async Task<T> WithRetryPolicy<T>(Func<Task<T>> action)
        {
            return await _retryPolicy.ExecuteAsync(action);
        }

        protected async Task WithRetryPolicy(Func<Task> action)
        {
            await _retryPolicy.ExecuteAsync(action);
        }

        protected CommandDefinition CreateCommand(string sql, object parameters = null)
        {
            return new CommandDefinition(sql, parameters, transaction: _transaction, commandTimeout: _commandTimeout);
        }

        public void SetTransaction(IDbTransaction transaction)
        {
            _transaction = transaction;
        }
    }
}
