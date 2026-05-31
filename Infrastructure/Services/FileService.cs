using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using FloraCore.Infrastructure.Repositories;

using FloraCore.Domain.Exceptions;

namespace FloraCore.Infrastructure.Services;

public class FileService(
    IGenericRepository<FileMetadata, Guid> repository, 
    IWebHostEnvironment environment, 
    ICurrentUserService currentUserService, 
    IConfiguration configuration,
    IResourceManager resourceManager) : IFileService
{
    private readonly IGenericRepository<FileMetadata, Guid> _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IWebHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ICurrentUserService _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    private readonly string _uploadFolder = (configuration ?? throw new ArgumentNullException(nameof(configuration)))["FileStorage:UploadFolder"] ?? "uploads";

    public async Task<FileMetadata> UploadFileAsync(IFormFile file, string? objectId = null, string? objectType = null, bool isPublic = true)
    {
        if (file == null || file.Length == 0) throw new ArgumentException(_resourceManager.GetString("FileIsEmpty"));
        if (string.IsNullOrEmpty(objectId)) throw new ArgumentException(_resourceManager.GetString("ObjectIdIsEmpty"));
        if (string.IsNullOrEmpty(objectType)) throw new ArgumentException(_resourceManager.GetString("ObjectTypeIsEmpty"));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".docx" };
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException(string.Format(_resourceManager.GetString("FileExtensionNotAllowed"), extension));

        if (file.Length > 10 * 1024 * 1024)
            throw new ArgumentException(_resourceManager.GetString("FileSizeExceedsLimit"));

        

        var uploadPath = Path.Combine(_environment.ContentRootPath, _uploadFolder);
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        var storedName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, storedName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var metadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            StoredName = storedName,
            FilePath = filePath,
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

        // Security Check: Only the owner can delete the file
        if (metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException(_resourceManager.GetString("UnauthorizedToDeleteFile"));

        if (File.Exists(metadata.FilePath))
        {
            File.Delete(metadata.FilePath);
        }

        await _repository.DeleteAsync(metadata);
        return true;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileAsync(Guid fileId)
    {
        var metadata = await _repository.GetByIdAsync(fileId);
        if (metadata == null || !File.Exists(metadata.FilePath))
            throw new FileNotFoundException(_resourceManager.GetString("FileNotFound"));

        // Security Check: Access is allowed if file is public OR current user is the owner
        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException(_resourceManager.GetString("UnauthorizedToAccessPrivateFile"));

        var bytes = await File.ReadAllBytesAsync(metadata.FilePath);
        return (bytes, metadata.ContentType, metadata.FileName);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileByObjectIdAsync(string objectId)
    {
        var files = await _repository.FindAsync(f => f.ObjectId == objectId);
        // Lấy file mới nhất dựa trên thời gian tải lên
        var metadata = files.OrderByDescending(f => f.UploadedAt).FirstOrDefault();

        if (metadata == null)
            throw new FileNotFoundException(string.Format(_resourceManager.GetString("NoDatabaseEntryForObjectId"), objectId));

        if (!File.Exists(metadata.FilePath))
            throw new FileNotFoundException(string.Format(_resourceManager.GetString("PhysicalFileMissing"), metadata.FilePath));

        // Security Check: Access is allowed if file is public OR current user is the owner
        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException(_resourceManager.GetString("UnauthorizedToAccessPrivateFile"));

        var bytes = await File.ReadAllBytesAsync(metadata.FilePath);
        return (bytes, metadata.ContentType, metadata.FileName);
    }
}
