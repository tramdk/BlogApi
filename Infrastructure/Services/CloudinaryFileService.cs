using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Domain.Entities;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Infrastructure.Services;

public class CloudinaryFileService : IFileService
{
    private readonly IGenericRepository<FileMetadata, Guid> _repository;
    private readonly Cloudinary _cloudinary;
    private readonly ICurrentUserService _currentUserService;
    private readonly HttpClient _httpClient;

    public CloudinaryFileService(
        IGenericRepository<FileMetadata, Guid> repository,
        IOptions<CloudinarySettings> config,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        var acc = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);
        _cloudinary = new Cloudinary(acc);
        _httpClient = new HttpClient();
    }

    public async Task<FileMetadata> UploadFileAsync(IFormFile file, string? objectId = null, string? objectType = null, bool isPublic = true)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("File is empty");
        if (string.IsNullOrEmpty(objectId)) throw new ArgumentException("objectId is empty");
        if (string.IsNullOrEmpty(objectType)) throw new ArgumentException("objectType is empty");

        var uploadResult = new RawUploadResult();
        using (var stream = file.OpenReadStream())
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "blog_api",
                PublicId = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(file.FileName)}"
            };
            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }

        if (uploadResult.Error != null)
            throw new Exception(uploadResult.Error.Message);

        var metadata = new FileMetadata
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            FileName = file.FileName,
            StoredName = uploadResult.PublicId,
            FilePath = uploadResult.SecureUrl.ToString(),
            PublicId = uploadResult.PublicId,
            Url = uploadResult.SecureUrl.ToString(),
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

        if (Guid.TryParse(objectId, out var idAsGuid))
        {
            return await query
                .Where(f => (f.ObjectId == objectId || f.Id == idAsGuid) && (f.IsPublic || f.UploadedById == currentUserId))
                .ToListAsync();
        }

        return await query
            .Where(f => f.ObjectId == objectId && (f.IsPublic || f.UploadedById == currentUserId))
            .ToListAsync();
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        var metadata = await _repository.GetByIdAsync(fileId);
        if (metadata == null) return false;

        if (metadata.UploadedById != _currentUserService.UserId)
            throw new UnauthorizedAccessException("You are not authorized to delete this file.");

        try
        {
            var deletionParams = new DeletionParams(metadata.PublicId ?? metadata.StoredName);
            await _cloudinary.DestroyAsync(deletionParams);
        }
        catch (Exception)
        {
            // Log warning or info: physical file deletion failed or already deleted
        }

        await _repository.DeleteAsync(metadata);
        return true;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileAsync(Guid fileId)
    {
        var metadata = await _repository.GetByIdAsync(fileId);
        if (metadata == null) throw new FileNotFoundException("File metadata not found");

        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new UnauthorizedAccessException("You are not authorized to access this private file.");

        var fileUrl = metadata.Url ?? metadata.FilePath;
        var bytes = await _httpClient.GetByteArrayAsync(fileUrl);
        
        return (bytes, metadata.ContentType, metadata.FileName);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileByObjectIdAsync(string objectId)
    {
        var files = await _repository.FindAsync(f => f.ObjectId == objectId);
        var metadata = files.OrderByDescending(f => f.UploadedAt).FirstOrDefault();

        if (metadata == null)
            throw new FileNotFoundException($"No database entry found for ObjectId: {objectId}");

        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new UnauthorizedAccessException("You are not authorized to access this private file.");

        var fileUrl = metadata.Url ?? metadata.FilePath;
        var bytes = await _httpClient.GetByteArrayAsync(fileUrl);

        return (bytes, metadata.ContentType, metadata.FileName);
    }
}
