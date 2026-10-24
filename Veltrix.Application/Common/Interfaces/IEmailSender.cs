namespace Veltrix.Application.Common.Interfaces;

/// <summary>
/// MVP implementation only writes the email to the log.
/// A real SMTP sender can be plugged in later (optional "email notifications" feature).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
