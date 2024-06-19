using AutoMapper;
using MyAzureFunctionApp.Models;
using MyAzureFunctionApp.Models.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Author, AuthorDto>().ReverseMap();
        CreateMap<Book, BookDto>().ReverseMap();
        CreateMap<Category, CategoryDto>().ReverseMap();
    }
}
