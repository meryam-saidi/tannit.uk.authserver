namespace Tannit.AuthServer.Models.DTOs
{
    public class AuthResult
    {
        public bool Success { get; private set; }
        public string? Token { get; private set; }
        public string? RefreshToken { get; private set; }
        public IEnumerable<string>? Errors { get; private set; }

        // Méthode statique pour succès
        public static AuthResult SuccessResult(string token, string refreshToken = null)
        {
            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                Errors = null
            };
        }

        // Méthode statique pour échec
        public static AuthResult Fail(params string[] errors)
        {
            return new AuthResult
            {
                Success = false,
                Token = null,
                RefreshToken = null,
                Errors = errors
            };
        }
    }
}
