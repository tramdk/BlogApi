using System.Threading.Tasks;

namespace FloraCore.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="body">The HTML or plain text body content of the email.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendEmailAsync(string to, string subject, string body);
}
