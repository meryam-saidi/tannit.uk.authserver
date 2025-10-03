using Dapper;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Tannit.AuthServer.Services;
using System.Data;
using Tannit.AuthServer.Models.Entities;

namespace Tannit.AuthServer.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _db;
        private readonly PasswordService _passwordService;
        private static readonly List<User> _users = new List<User>();

        public UserRepository(IDbConnection db, PasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = "SELECT * FROM Users WHERE Email = @Email";
            return await _db.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            const string sql = "SELECT Id, Email, Balance FROM Users WHERE Id = @Id";
            return await _db.QuerySingleOrDefaultAsync<User>(sql, new { Id = userId });
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null) return false;


            return _passwordService.VerifyPassword(password, user.PasswordHash);
        }

        public async Task AddUserAsync(User user)
        {
            var sql = @"
        INSERT INTO Users (Id, Email, PasswordHash, Role, Balance)
        VALUES (@Id, @Email, @PasswordHash,@Role, @Balance)";

            await _db.ExecuteAsync(sql, user);
        }

        public Task UpdateUserAsync(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                // Mettre à jour les propriétés de l'utilisateur existant
                existingUser.Email = user.Email;
                existingUser.PasswordHash = user.PasswordHash;
                existingUser.Role = user.Role;
                existingUser.Balance = user.Balance;
                existingUser.IsEmailVerified = user.IsEmailVerified;
                // ... mettez à jour toutes les autres propriétés pertinentes
            }
            return Task.CompletedTask;
        }

        public async Task UpdateBalanceAsync(User user)
        {
            var sql = "UPDATE Users SET Balance = @Balance WHERE Id = @Id";
            await _db.ExecuteAsync(sql, new { user.Balance, user.Id });
        }

        public async Task StoreEmailVerification(Guid id, string email, string code)
        {
            var sql = @"INSERT INTO EmailVerifications (Id, Email, Code, Expiration, IsUsed)
                VALUES (@Id, @Email, @Code, @Expiration, 0)";
            await _db.ExecuteAsync(sql, new
            {
                Id = id,
                Email = email,
                Code = code,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });
        }

    }
}
