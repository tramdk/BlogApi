using System;

namespace FloraCore.Domain.Entities;

public class FileMetadata
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty; // For local: Unique name on disk. For Cloud: PublicId
    public string FilePath { get; set; } = string.Empty; // For local: absolute path. For Cloud: Secure Url
    public string? PublicId { get; set; } // Cloudinary PublicId
    public string? Url { get; set; } // Cloudinary Secure Url
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Polymorphic association
    public string? ObjectId { get; set; } // ID of the related object (Product, Post, User, etc.)
    public string? ObjectType { get; set; } // Type name (e.g., "Product", "Post", "Avatar")

    public Guid? UploadedById { get; set; }
    public AppUser? UploadedBy { get; set; }

    public bool IsPublic { get; set; } = true;
}
