using Tannit.AuthServer.Models.Entities;

namespace Tannit.AuthServer.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid userId);
        Task<bool> ValidateCredentialsAsync(string email, string password);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task UpdateBalanceAsync(User user);
    }
}
