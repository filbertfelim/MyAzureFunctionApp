using FluentValidation;
using MyAzureFunctionApp.Models.DTOs;

namespace MyAzureFunctionApp.Validators
{
    public class BookDtoValidator : AbstractValidator<BookDto>
    {
        public BookDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Book title is required.")
                .Length(1, 255).WithMessage("Book title must be between 1 and 255 characters.");

            RuleFor(x => x.AuthorId)
                .NotEmpty().WithMessage("Author ID is required.");

            RuleFor(x => x.CategoryIds)
                .NotEmpty().WithMessage("At least one category ID is required.")
                .Must(x => x.Count > 0).WithMessage("At least one category ID is required.");
        }
    }
}
