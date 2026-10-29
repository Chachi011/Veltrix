using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Interfaces;

namespace Veltrix.Infrastructure.Email;

/// <summary>
/// Development "mailbox": the email is written to the console / log file.
/// </summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL to {To} | {Subject} | {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
