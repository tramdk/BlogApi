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

public class FileService : IFileService
{
    private readonly IGenericRepository<FileMetadata, Guid> _repository;
    private readonly IWebHostEnvironment _environment;
    private readonly ICurrentUserService _currentUserService;
    private readonly string _uploadFolder;

    public FileService(
        IGenericRepository<FileMetadata, Guid> repository, 
        IWebHostEnvironment environment, 
        ICurrentUserService currentUserService, 
        IConfiguration configuration)
    {
        _repository = repository;
        _environment = environment;
        _currentUserService = currentUserService;
        _uploadFolder = configuration["FileStorage:UploadFolder"] ?? "uploads";
    }

    public async Task<FileMetadata> UploadFileAsync(IFormFile file, string? objectId = null, string? objectType = null, bool isPublic = true)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("File is empty");
        if (string.IsNullOrEmpty(objectId)) throw new ArgumentException("objectId is empty");
        if (string.IsNullOrEmpty(objectType)) throw new ArgumentException("objectType is empty");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".docx" };
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException($"File extension {extension} is not allowed.");

        if (file.Length > 10 * 1024 * 1024)
            throw new ArgumentException("File size exceeds 10MB limit.");

        

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
            throw new AccessDeniedException("You are not authorized to delete this file.");

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
            throw new FileNotFoundException("File not found");

        // Security Check: Access is allowed if file is public OR current user is the owner
        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException("You are not authorized to access this private file.");

        var bytes = await File.ReadAllBytesAsync(metadata.FilePath);
        return (bytes, metadata.ContentType, metadata.FileName);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileByObjectIdAsync(string objectId)
    {
        var files = await _repository.FindAsync(f => f.ObjectId == objectId);
        // Lấy file mới nhất dựa trên thời gian tải lên
        var metadata = files.OrderByDescending(f => f.UploadedAt).FirstOrDefault();

        if (metadata == null)
            throw new FileNotFoundException($"No database entry found for ObjectId: {objectId}");

        if (!File.Exists(metadata.FilePath))
            throw new FileNotFoundException($"Physical file missing at: {metadata.FilePath}");

        // Security Check: Access is allowed if file is public OR current user is the owner
        if (!metadata.IsPublic && metadata.UploadedById != _currentUserService.UserId)
            throw new AccessDeniedException("You are not authorized to access this private file.");

        var bytes = await File.ReadAllBytesAsync(metadata.FilePath);
        return (bytes, metadata.ContentType, metadata.FileName);
    }
}
