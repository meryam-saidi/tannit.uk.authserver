using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace Tannit.AuthServer.Repositories
{
    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly IDbConnection _db;

        public EmailVerificationRepository(IConfiguration config)
        {
            _db = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        }

        public async Task<EmailVerification?> GetByEmailAndCodeAsync(string email, string code)
        {
            var sql = @"
                SELECT * FROM EmailVerifications 
                WHERE Email = @Email 
                AND Code = @Code 
                AND Expiration > @Now 
                AND IsUsed = 0
                ORDER BY Expiration DESC
                
            ";

            return await _db.QueryFirstOrDefaultAsync<EmailVerification>(
                sql,
                new { Email = email, Code = code, Now = DateTime.UtcNow }
            );
        }


        public async Task<EmailVerification?> GetByEmailAsync(string email)
        {
            var sql = "SELECT * FROM EmailVerifications WHERE Email = @Email";
            return await _db.QueryFirstOrDefaultAsync<EmailVerification>(sql, new { Email = email });
        }

        public async Task AddAsync(EmailVerification verification)
        {
            var sql = "INSERT INTO EmailVerifications (Id, Email, Code, Expiration, IsUsed) VALUES (@Id, @Email, @Code, @Expiration, @IsUsed)";
            await _db.ExecuteAsync(sql, verification);
        }

        public async Task DeleteAsync(string email)
        {
            var sql = "DELETE FROM EmailVerifications WHERE Email = @Email";
            await _db.ExecuteAsync(sql, new { Email = email });
        }

        public async Task UpsertAsync(EmailVerification code)
        {
            // D'abord, on vérifie s'il existe déjà un code pour cet email
            var existing = await _db.QueryFirstOrDefaultAsync<EmailVerification>(
                "SELECT * FROM EmailVerifications WHERE Email = @Email", new { code.Email });

            if (existing != null)
            {
                // Mise à jour du code existant
                await _db.ExecuteAsync(
                    @"UPDATE EmailVerifications 
                  SET Code = @Code, Expiration = @Expiration, IsUsed = 0 
                  WHERE Email = @Email",
                    new
                    {
                        code.Code,
                        code.Expiration,
                        code.Email
                    });
            }
            else
            {
                // Insertion d’un nouveau code
                await _db.ExecuteAsync(
                    @"INSERT INTO EmailVerifications (Email, Code, Expiration, IsUsed) 
                    VALUES (@Email, @Code, @Expiration, 0)", // on initialise IsUsed à false (0)
                    code);
            }
        }

        public async Task MarkAsUsedAsync(Guid id)
        {
            var sql = @"UPDATE EmailVerifications SET IsUsed = 1 WHERE Id = @Id";
            await _db.ExecuteAsync(sql, new { Id = id });
        }

        public async Task DeleteExpiredCodesAsync()
        {
            var sql = @"DELETE FROM EmailVerifications 
                WHERE ExpiresAt < @Now";
            await _db.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
        }


    }
}
