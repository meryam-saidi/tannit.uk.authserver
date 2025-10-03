namespace Tannit.AuthServer.Repositories
{
    public interface IEmailVerificationRepository
    {
        Task<EmailVerification?> GetByEmailAndCodeAsync(string email, string code);

        Task<EmailVerification?> GetByEmailAsync(string email);
        Task AddAsync(EmailVerification verification);
        Task DeleteAsync(string email);
        Task UpsertAsync(EmailVerification verification);
        Task MarkAsUsedAsync(Guid id);
        Task DeleteExpiredCodesAsync();
    }
}
