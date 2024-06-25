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
                .Length(1, 255).WithMessage("Book title must be between 1 and 255 characters.")
                .Matches(@"^[^\s][\w\s\.\-,'!@#$%^&*()_+=]+[^\s]$").WithMessage("Book title must be a valid string without leading, trailing, or consecutive spaces and must not contain semicolons.")
                .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("Book title must not be just whitespace.")
                .Must(title => !title.Contains(";")).WithMessage("Book title must not contain semicolons.")
                .Custom((title, context) => 
                {
                    // Trim leading and trailing whitespace
                    var trimmedTitle = title.Trim();

                    // Check for double spaces
                    if (trimmedTitle.Contains("  "))
                    {
                        context.AddFailure("Title", "Book title must not contain consecutive spaces.");
                    }
                    else
                    {
                        // Update the title with trimmed value
                        context.InstanceToValidate.Title = trimmedTitle;
                    }
                });

            RuleFor(x => x.AuthorId)
                .GreaterThan(0).WithMessage("Author ID must be a positive integer.");

            RuleFor(x => x.CategoryIds)
                .NotEmpty().WithMessage("At least one category ID is required.")
                .Must(x => x.Count > 0).WithMessage("At least one category ID is required.")
                .ForEach(id =>
                {
                    id.GreaterThan(0).WithMessage("Category ID must be a positive integer.");
                });
        }
    }
}
