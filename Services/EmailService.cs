using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration cfg, ILogger<EmailService> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

public async Task SendEmailAsync(string recipient, string subject, string body, bool isHtml = false)
{
    var host = _cfg["Mail:Host"] ?? "smtp.gmail.com";
    var port = int.Parse(_cfg["Mail:Port"] ?? "587"); // domyślnie 587
    var user = _cfg["Mail:User"] ?? throw new InvalidOperationException("Mail:User brak");
    var pass = _cfg["Mail:Password"]
              ?? Environment.GetEnvironmentVariable("SMTP_PASS")
              ?? throw new InvalidOperationException("Brak hasła SMTP");
    var display = _cfg["Mail:DisplayName"] ?? user;

    // "StartTls" | "SslOnConnect" | "Auto"
    var sslModeStr = _cfg["Mail:SslMode"] ?? "Auto";
    var disableCrl = bool.TryParse(_cfg["Mail:DisableCertRevocationCheck"], out var d) && d;

    var sslMode = sslModeStr switch
    {
        "StartTls" => SecureSocketOptions.StartTls,
        "SslOnConnect" => SecureSocketOptions.SslOnConnect,
        "Auto" => SecureSocketOptions.Auto,
        _ => SecureSocketOptions.Auto
    };

    var msg = new MimeMessage();
    msg.From.Add(new MailboxAddress(display, user));
    msg.To.Add(MailboxAddress.Parse(recipient));
    msg.Subject = subject;
    msg.Date = DateTimeOffset.Now;

    // ✅ multipart/alternative: HTML + fallback plain-text
    var builder = new BodyBuilder();
    if (isHtml)
    {
        builder.HtmlBody = body;
        builder.TextBody = ToPlainText(body); // fallback dla klientów bez HTML
    }
    else
    {
        builder.TextBody = body;
    }
    msg.Body = builder.ToMessageBody();

    using var client = new SmtpClient
    {
        CheckCertificateRevocation = !disableCrl, // DEV: ustaw Mail:DisableCertRevocationCheck = true
        Timeout = 15000
    };

    try
    {
        client.AuthenticationMechanisms.Remove("XOAUTH2"); // używamy user+hasło

        await client.ConnectAsync(host, port, sslMode);
        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);

        _logger.LogInformation("E-mail wysłany do {Recipient}, HTML={IsHtml}", recipient, isHtml);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Błąd wysyłki e-maila do {Recipient}. Host={Host}, Port={Port}, SSL={Ssl}",
            recipient, host, port, sslMode);
        throw;
    }
}

/// <summary>
/// Bardzo prosta konwersja HTML → tekst (wystarczająca dla nagłówków/akapitów).
/// </summary>
private static string ToPlainText(string html)
{
    if (string.IsNullOrWhiteSpace(html)) return string.Empty;

    // nowe linie dla popularnych tagów blokowych
    html = Regex.Replace(html, @"</?(p|div|br|h[1-6]|li|ul|ol|tr|table)\b[^>]*>", m =>
    {
        var tag = m.Value.ToLowerInvariant();
        return tag.StartsWith("</") || tag.Contains("<br") ? Environment.NewLine : string.Empty;
    });

    // usuń resztę tagów
    html = Regex.Replace(html, "<.*?>", string.Empty);

    // znormalizuj białe znaki
    html = Regex.Replace(html, @"[ \t]+\n", "\n");
    html = Regex.Replace(html, @"\n{3,}", "\n\n");
    return html.Trim();
}



}
