using MailKit.Net.Smtp;
using MailKit.Security;
using Markdig;
using Microsoft.Extensions.Configuration;
using MimeKit;
using UserApp.Application.Common.Interfaces;

namespace UserApp.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var message = BuildMessage(toEmail, subject, body);
        await SendAsync(message);
    }

    public async Task SendTemplateAsync(string toEmail, string subject, string templateName, Dictionary<string, string> placeholders)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", templateName);

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Email template not found: {templatePath}");

        var markdown = await File.ReadAllTextAsync(templatePath);

        foreach (var (key, value) in placeholders)
        {
            markdown = markdown.Replace($"{{{{{key}}}}}", value);
        }

        var html = ConvertMarkdownToHtml(markdown);
        var message = BuildMessage(toEmail, subject, html);
        await SendAsync(message);
    }

    private static string ConvertMarkdownToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var contentHtml = Markdown.ToHtml(markdown, pipeline);

        return WrapInEmailTemplate(contentHtml);
    }

    private static string WrapInEmailTemplate(string contentHtml)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            </head>
            <body style="margin:0;padding:0;background-color:#f4f7fb;font-family:'Segoe UI',Arial,sans-serif;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f7fb;padding:40px 16px;">
                    <tr>
                        <td align="center">
                            <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.06);">
                                <tr>
                                    <td style="padding:40px 32px;">
                                        {contentHtml}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:20px 32px;background:#f8fafc;border-top:1px solid #e2e8f0;text-align:center;">
                                        <p style="margin:0;font-size:12px;color:#94a3b8;">&copy; UserApp. All rights reserved.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    private MimeMessage BuildMessage(string toEmail, string subject, string body)
    {
        var fromEmail = _config["Smtp:FromEmail"];
        var fromName = _config["Smtp:FromName"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();
        return message;
    }

    private async Task SendAsync(MimeMessage message)
    {
        var host = _config["Smtp:Host"];
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}