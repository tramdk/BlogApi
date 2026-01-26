using System;

namespace BlogApi.Domain.Entities;

public class FileMetadata
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty; // Unique name on disk
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Polymorphic association
    public string? ObjectId { get; set; } // ID of the related object (Product, Post, User, etc.)
    public string? ObjectType { get; set; } // Type name (e.g., "Product", "Post", "Avatar")

    public Guid? UploadedById { get; set; }
    public AppUser? UploadedBy { get; set; }
}
