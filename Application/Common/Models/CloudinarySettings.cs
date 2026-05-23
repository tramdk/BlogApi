namespace FloraCore.Application.Common.Models;

/// <summary>
/// Represents the Cloudinary settings.
/// </summary>
public class CloudinarySettings
{
    /// <summary>
    /// Gets or sets the cloud name.
    /// </summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API secret.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload folder.
    /// </summary>
    public string UploadFolder { get; set; } = "blog_api";
}
