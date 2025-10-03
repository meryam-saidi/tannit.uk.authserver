namespace Tannit.AuthServer.Services
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string email, string code);
        Task SendEmailAsync(string to, string subject, string body);

    }
}
