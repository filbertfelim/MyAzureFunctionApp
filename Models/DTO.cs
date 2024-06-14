namespace MyAzureFunctionApp.Models
{
    public class BookDto
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public AuthorDto Author { get; set; }
        public List<BookCategoryDto> BookCategories { get; set; }
    }

    public class AuthorDto
    {
        public int AuthorId { get; set; }
        public string Name { get; set; }
    }

    public class BookCategoryDto
    {
        public int BookId { get; set; }
        public CategoryDto Category { get; set; }
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
    }

    public class CreateUpdateBookDto
    {
        public string Title { get; set; }
        public int AuthorId { get; set; }
        public List<int> CategoryIds { get; set; } = new List<int>();
    }
}