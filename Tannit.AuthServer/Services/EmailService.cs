using System.Net.Mail;
using System.Net;
using Tannit.AuthServer.Services;
using Microsoft.Extensions.Configuration;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationCodeAsync(string email, string code)
    {
        var smtpClient = new SmtpClient("smtp.example.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("your@email.com", "password"),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage("your@email.com", email)
        {
            Subject = "Votre code de vérification",
            Body = $"Votre code est : {code}",
        };

        await smtpClient.SendMailAsync(mailMessage);
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpClient = new SmtpClient(_config["Smtp:Host"])
        {
            Port = int.Parse(_config["Smtp:Port"]),
            Credentials = new NetworkCredential(
                _config["Smtp:Username"],
                _config["Smtp:Password"]
            ),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_config["Smtp:From"]),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };

        mailMessage.To.Add(to);

        await smtpClient.SendMailAsync(mailMessage);
    }
}
