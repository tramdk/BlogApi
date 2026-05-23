using System.ComponentModel.DataAnnotations;

namespace FloraCore.Application.Common.Models;

/// <summary>
/// Represents the JWT settings.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Gets or sets the secret key.
    /// </summary>
    [Required]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuer.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiry minutes.
    /// </summary>
    [Required]
    [Range(1, 1440)] // 1 minute to 24 hours
    public int ExpiryMinutes { get; set; } = 30;
}
