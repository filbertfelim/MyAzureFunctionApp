using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class AuthorService : Service<Author>, IAuthorService
    {
        private readonly IDapperUnitOfWork _unitOfWork;

        public AuthorService(IDapperUnitOfWork unitOfWork) : base(unitOfWork.Authors)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Author> AddAsync(AuthorDto request)
        {
            var author = new Author { Name = request.Name };
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await _unitOfWork.Authors.AddAsync(author);
                await _unitOfWork.CommitAsync();
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<Author> UpdateAsync(int id, AuthorDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var author = await _unitOfWork.Authors.GetByIdAsync(id);
                if (author == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return null;
                }

                author.Name = request.Name;
                var result = await _unitOfWork.Authors.UpdateAsync(author);
                await _unitOfWork.CommitAsync();
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<Author> GetByIdAsync(int id)
        {
            return await _unitOfWork.Authors.GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var author = await _unitOfWork.Authors.GetByIdAsync(id);
                if (author != null)
                {
                     var books = await _unitOfWork.Books.GetAllAsync();
                    foreach (var book in books)
                    {
                        if (book.AuthorId == id)
                        {
                            await _unitOfWork.BookCategories.DeleteByBookIdAsync(book.BookId);
                            await _unitOfWork.Books.DeleteAsync(book.BookId);
                        }
                    }
                    await _unitOfWork.Authors.DeleteAsync(id);
                    await _unitOfWork.CommitAsync();
                }
                else
                {
                    await _unitOfWork.RollbackAsync();
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
