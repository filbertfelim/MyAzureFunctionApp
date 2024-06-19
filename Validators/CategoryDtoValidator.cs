using FluentValidation;
using MyAzureFunctionApp.Models.DTOs;

namespace MyAzureFunctionApp.Validators
{
    public class CategoryDtoValidator : AbstractValidator<CategoryDto>
    {
        public CategoryDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .Length(1, 255).WithMessage("Category name must be between 1 and 255 characters.");
        }
    }
}
