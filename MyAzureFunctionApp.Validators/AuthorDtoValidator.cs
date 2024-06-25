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
                .Length(1, 255).WithMessage("Author name must be between 1 and 255 characters.")
                .Matches(@"^[a-zA-Z]+( [a-zA-Z]+)*$").WithMessage("Author name must contain only alphabetic characters and single spaces between words.")
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Author name must not be just whitespace.")
                .Custom((name, context) => 
                {
                    // Trim leading and trailing whitespace
                    var trimmedName = name.Trim();

                    // Check for double spaces
                    if (trimmedName.Contains("  "))
                    {
                        context.AddFailure("Name", "Author name must not contain consecutive spaces.");
                    }
                    else
                    {
                        // Update the name with trimmed value
                        context.InstanceToValidate.Name = trimmedName;
                    }
                });
        }
    }
}
