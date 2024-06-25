using System.Threading.Tasks;

namespace MyAzureFunctionApp.Repositories
{
    public interface IDapperUnitOfWork : IDisposable
    {
        IBookRepository Books { get; }
        IAuthorRepository Authors { get; }
        ICategoryRepository Categories { get; }
        IBookCategoryRepository BookCategories { get; }

        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
