using System.Threading.Tasks;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;
using MyAzureFunctionApp.Repositories;

namespace MyAzureFunctionApp.Services
{
    public class AuthorService : Service<Author>, IAuthorService
    {
        private readonly IAuthorRepository _authorRepository;

        public AuthorService(IAuthorRepository authorRepository) : base(authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public async Task<Author> AddAsync(AuthorDto request)
        {
            var author = new Author { Name = request.Name };
            return await _authorRepository.AddAsync(author);
        }

        public async Task<Author> UpdateAsync(int id, AuthorDto request)
        {
            var author = await _authorRepository.GetByIdAsync(id);
            if (author == null)
            {
                return null; // Author not found
            }

            author.Name = request.Name;
            return await _authorRepository.UpdateAsync(author);
        }

        public async Task<Author> GetByIdAsync(int id)
        {
            return await _authorRepository.GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            var author = await _authorRepository.GetByIdAsync(id);
            if (author != null)
            {
                await _authorRepository.DeleteAsync(id);
            }
           
        }
    }
}
