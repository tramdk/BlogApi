using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FloraCore.Tests.Mocks;

public class FakeFileService : IFileService
{
    private readonly IGenericRepository<FileMetadata, Guid> _repository;
    private readonly ICurrentUserService _currentUserService;

    public FakeFileService(IGenericRepository<FileMetadata, Guid> repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<FileMetadata> UploadFileAsync(IFormFile file, string? objectId = null, string? objectType = null, bool isPublic = true)
    {
        var metadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            StoredName = $"fake_{Guid.NewGuid()}",
            FilePath = "https://res.cloudinary.com/demo/image/upload/sample.jpg",
            PublicId = $"fake_{Guid.NewGuid()}",
            Url = "https://res.cloudinary.com/demo/image/upload/sample.jpg",
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow,
            ObjectId = objectId,
            ObjectType = objectType,
            UploadedById = _currentUserService.UserId,
            IsPublic = isPublic
        };
        await _repository.AddAsync(metadata);
        return metadata;
    }

    public async Task<IEnumerable<FileMetadata>> GetFilesByObjectAsync(string objectId, string objectType)
    {
        var currentUserId = _currentUserService.UserId;
        return await _repository.FindAsync(f => f.ObjectId == objectId && f.ObjectType == objectType && (f.IsPublic || f.UploadedById == currentUserId));
    }

    public async Task<List<FileMetadata>> GetFilesByObjectIdAsync(string objectId)
    {
        var currentUserId = _currentUserService.UserId;
        var query = _repository.GetQueryable();
        return query.Where(f => f.ObjectId == objectId && (f.IsPublic || f.UploadedById == currentUserId)).ToList();
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        var metadata = await _repository.GetByIdAsync(fileId);
        if (metadata == null) return false;
        await _repository.DeleteAsync(metadata);
        return true;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileAsync(Guid fileId)
    {
        var metadata = await _repository.GetByIdAsync(fileId);
        if (metadata == null) throw new FileNotFoundException();

        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException("private file");

        return (new byte[] { 1, 2, 3, 4 }, metadata.ContentType, metadata.FileName);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileByObjectIdAsync(string objectId)
    {
        var files = await _repository.FindAsync(f => f.ObjectId == objectId);
        var metadata = files.FirstOrDefault();
        if (metadata == null) throw new FileNotFoundException();

        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException("private file");

        return (new byte[] { 1, 2, 3, 4 }, metadata.ContentType, metadata.FileName);
    }
}
