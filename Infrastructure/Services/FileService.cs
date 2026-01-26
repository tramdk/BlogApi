using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BlogApi.Infrastructure.Repositories;
using UUIDNext;

namespace BlogApi.Infrastructure.Services;

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

    public async Task<FileMetadata> UploadFileAsync(IFormFile file, string? objectId = null, string? objectType = null)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("File is empty");
        if (string.IsNullOrEmpty(objectId)) throw new ArgumentException("objectId is empty");
        if (string.IsNullOrEmpty(objectType)) throw new ArgumentException("objectType is empty");
        

        var uploadPath = Path.Combine(_environment.ContentRootPath, _uploadFolder);
        if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

        var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadPath, storedName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var metadata = new FileMetadata
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            FileName = file.FileName,
            StoredName = storedName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow,
            ObjectId = objectId,
            ObjectType = objectType,
            UploadedById = _currentUserService.UserId
        };

        await _repository.AddAsync(metadata);

        return metadata;
    }

    public async Task<IEnumerable<FileMetadata>> GetFilesByObjectAsync(string objectId, string objectType)
    {
        return await _repository.FindAsync(f => f.ObjectId == objectId && f.ObjectType == objectType);
    }
    
    public async Task<List<FileMetadata>> GetFilesByObjectIdAsync(string objectId)
    {
        if (Guid.TryParse(objectId, out var idAsGuid))
        {
            return await _repository.GetQueryable()
                .Where(f => f.ObjectId == objectId || f.Id == idAsGuid)
                .ToListAsync();
        }
        
        return await _repository.GetQueryable()
            .Where(f => f.ObjectId == objectId)
            .ToListAsync();
    }

    public async Task<bool> DeleteFileAsync(Guid fileId)
    {
        var metadata = await _repository.GetByIdAsync(fileId);
        if (metadata == null) return false;

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

        var bytes = await File.ReadAllBytesAsync(metadata.FilePath);
        return (bytes, metadata.ContentType, metadata.FileName);
    }
}
