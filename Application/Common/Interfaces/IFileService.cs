using BlogApi.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApi.Application.Common.Interfaces;

public interface IFileService
{
    Task<FileMetadata> UploadFileAsync(IFormFile file, string? objectId = null, string? objectType = null);
    Task<IEnumerable<FileMetadata>> GetFilesByObjectAsync(string objectId, string objectType);
    Task<List<FileMetadata>> GetFilesByObjectIdAsync(string objectId);
    Task<bool> DeleteFileAsync(Guid fileId);
    Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileAsync(Guid fileId);
    Task<(byte[] Bytes, string ContentType, string FileName)> DownloadFileByObjectIdAsync(string objectId);
}
