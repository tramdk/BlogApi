using AutoMapper;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
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
