using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace SmartMarketingPlatform.Common.Notifications;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage(IReadOnlyList<string> To, string Subject, string HtmlBody);

public sealed class EmailException(string message) : Exception(message);

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; init; } = "File";             // File | Smtp
    public string OutboxPath { get; init; } = "App_Data/outbox";
    public string FromAddress { get; init; } = "reports@smartmarketing.local";
    public string FromName { get; init; } = "Smart Marketing";
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; } = 587;
    public bool SmtpEnableSsl { get; init; } = true;
    public string? SmtpUser { get; init; }
    public string? SmtpPassword { get; init; }
}

// Development: every email is written as an .html file you can open in a browser.
public sealed class FileEmailSender(IOptions<EmailOptions> options, IHostEnvironment environment, ILogger<FileEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(environment.ContentRootPath, options.Value.OutboxPath);
        Directory.CreateDirectory(folder);

        var file = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}.html");
        var header = $"<!-- To: {WebUtility.HtmlEncode(string.Join(", ", message.To))} | Subject: {WebUtility.HtmlEncode(message.Subject)} -->\n";
        await File.WriteAllTextAsync(file, header + message.HtmlBody, new UTF8Encoding(false), cancellationToken);
        logger.LogInformation("Email '{Subject}' to {Count} recipient(s) written to {File}", message.Subject, message.To.Count, file);
    }
}

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.SmtpHost))
            throw new EmailException("Email:SmtpHost is not set.");

        using var mail = new MailMessage
        {
            From = new MailAddress(o.FromAddress, o.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        foreach (var to in message.To)
            mail.To.Add(to);

        using var client = new SmtpClient(o.SmtpHost, o.SmtpPort)
        {
            EnableSsl = o.SmtpEnableSsl,
            Credentials = string.IsNullOrWhiteSpace(o.SmtpUser) ? null : new NetworkCredential(o.SmtpUser, o.SmtpPassword)
        };

        try
        {
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (SmtpException ex)
        {
            throw new EmailException($"The email could not be sent: {ex.Message}");
        }
    }
}
