using FluentValidation;
using MyAzureFunctionApp.Models.DTOs;

namespace MyAzureFunctionApp.Validators
{
    public class AuthorDtoValidator : AbstractValidator<AuthorDto>
    {
        public AuthorDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Author name is required.")
                .Length(1, 255).WithMessage("Author name must be between 1 and 255 characters.");
        }
    }
}
