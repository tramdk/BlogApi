using System;

namespace BlogApi.Application.Common.Models;

public class FileResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ObjectId { get; set; }
    public string? ObjectType { get; set; }
    public string ViewUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}
