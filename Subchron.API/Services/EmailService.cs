using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

public class EmailService
{
    private readonly SmtpSettings? _s;

    public EmailService(IOptions<SmtpSettings> smtp)
    {
        SmtpSettings? s = null;
        try
        {
            s = smtp?.Value;
            if (string.IsNullOrWhiteSpace(s?.Host))
                s = null;
        }
        catch
        {
            s = null;
        }
        _s = s;
    }

    public async Task SendAsync(string toEmail, string subject, string html)
    {
        if (_s == null)
            throw new InvalidOperationException("SMTP is not configured.");
        using var client = new SmtpClient(_s.Host, _s.Port)
        {
            EnableSsl = _s.EnableSsl,
            Credentials = new NetworkCredential(_s.Username ?? "", _s.Password ?? "")
        };

        using var msg = new MailMessage
        {
            From = new MailAddress(_s.FromEmail ?? "", _s.FromName ?? ""),
            Subject = subject,
            Body = html,
            IsBodyHtml = true
        };

        msg.To.Add(toEmail);
        await client.SendMailAsync(msg);
    }
}
