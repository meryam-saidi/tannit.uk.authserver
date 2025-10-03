using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Tannit.AuthServer.Models.DTOs;
using Tannit.AuthServer.Models.Entities;
using Tannit.AuthServer.Repositories;
using Tannit.AuthServer.Services;
using static Tannit.AuthServer.Models.DTOs.AuthRequests;

namespace Tannit.AuthServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthService authService, IUserRepository userRepository, IConfiguration config, IJwtService jwtService)
        {
            _authService = authService;
            _userRepository = userRepository;
            _config = config;
            _jwtService = jwtService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                await _authService.RegisterAsync(request.Email, request.Password);
                return Ok(new { message = "Inscription réussie. Un code de vérification a été envoyé à votre e-mail." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var authResult = await _authService.LoginAsync(loginRequest.Email, loginRequest.Password);

            if (!authResult.Success)
            {
                return Unauthorized(new { errors = authResult.Errors });
            }

            return Ok(new
            {
                token = authResult.Token,
                //refreshToken = authResult.RefreshToken
            });
        }


        //[Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("User ID missing in token");

            var userId = Guid.Parse(userIdClaim.Value);
            var profile = await _authService.GetProfileAsync(userId);

            if (profile == null)
                return NotFound();

            return Ok(profile);

        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var token = await _authService.VerifyEmailAsync(request.Email, request.Code);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //[Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize]
        [HttpPost("claim-reward")]
        public async Task<IActionResult> ClaimReward()
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized("User ID missing in token");

            var userId = Guid.Parse(userIdClaim.Value);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            // Exemple : empêcher plusieurs claims si besoin (à adapter selon ton besoin)
            if (user.Balance >= 40) return BadRequest("Reward already claimed");

            user.Balance += 40;
            await _userRepository.UpdateBalanceAsync(user);

            return Ok(new { newBalance = user.Balance });
        }


        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse([FromQuery] string? returnUrl)
        {
            var result = await HttpContext.AuthenticateAsync("Google");

            if (!result.Succeeded)
                return Unauthorized();

            var claims = result.Principal!.Identities.FirstOrDefault()!.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email not provided by Google.");

            // Vérifie si l’utilisateur existe déjà
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = null, // pas de mot de passe car login via Google
                    Role = "User",
                    Balance = 0
                };

                await _userRepository.AddUserAsync(user);
            }

            var jwt = _jwtService.GenerateToken(user);

            //return Ok(new { token = jwt });

            var safeReturn = string.IsNullOrWhiteSpace(returnUrl)
                ? $"{Request.Scheme}://localhost:3000/GoogleCallback" // fallback to FRONTEND
                : returnUrl;

            return Redirect($"{safeReturn}#token={Uri.EscapeDataString(jwt)}");
            
        }
    }
}
