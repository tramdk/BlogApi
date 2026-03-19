using AutoMapper;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Application.Features.Products.Queries;
using BlogApi.Application.Features.Users.Queries;
using BlogApi.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace BlogApi.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Files
        CreateMap<FileMetadata, FileResponse>()
            .ForMember(dest => dest.ViewUrl, opt => opt.MapFrom<FileViewUrlResolver>())
            .ForMember(dest => dest.DownloadUrl, opt => opt.MapFrom<FileDownloadUrlResolver>());

        // Posts
        CreateMap<Post, PostDetailDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.FullName : null));
            
        CreateMap<Post, PostDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.FullName : null));

        // Products
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));
        
        CreateMap<ProductReview, ReviewDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Anonymous"));

        // Users
        CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles handled manually in handler usually
    }
}

public class FileViewUrlResolver : IValueResolver<FileMetadata, FileResponse, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FileViewUrlResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Resolve(FileMetadata source, FileResponse destination, string destMember, ResolutionContext context)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;

        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/api/Files";
        return $"{baseUrl}/view/object/{source.ObjectId}";
    }
}

public class FileDownloadUrlResolver : IValueResolver<FileMetadata, FileResponse, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FileDownloadUrlResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Resolve(FileMetadata source, FileResponse destination, string destMember, ResolutionContext context)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;

        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/api/Files";
        return $"{baseUrl}/download/object/{source.ObjectId}";
    }
}
