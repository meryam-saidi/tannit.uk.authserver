using Microsoft.AspNetCore.Mvc;
using Tannit.AuthServer.Models.DTOs;

namespace Tannit.AuthServer.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(string email, string password);
        Task<AuthResult> LoginAsync(string email, string password);
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
        Task<string> VerifyEmailAsync(string email, string code);

    }
}
