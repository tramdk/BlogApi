using System.ComponentModel.DataAnnotations;

namespace FloraCore.Application.Common.Models;

public class FileMetadataRequest
{
    [Required]
    public string ObjectId { get; set; } = string.Empty;
}
