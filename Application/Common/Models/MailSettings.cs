namespace FloraCore.Application.Common.Models;

/// <summary>
/// Configuration options for the Mail Service (SMTP/Gmail).
/// </summary>
public class MailSettings
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "MailSettings";

    /// <summary>
    /// Gets or sets the SMTP server address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the name of the sender.
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the sender.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password or app-password for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
