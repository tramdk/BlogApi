using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Common.Models;

public class FileMetadataRequest
{
    [Required]
    public string ObjectId { get; set; } = string.Empty;
}
