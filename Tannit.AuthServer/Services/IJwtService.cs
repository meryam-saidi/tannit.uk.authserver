using Tannit.AuthServer.Models.Entities;

namespace Tannit.AuthServer.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
