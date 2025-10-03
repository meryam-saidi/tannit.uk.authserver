using Microsoft.AspNetCore.Identity;
using System;
using System.Data;
using Tannit.AuthServer.Models.Entities;
using Tannit.AuthServer.Models.DTOs;
using Tannit.AuthServer.Repositories;
using static Duende.IdentityServer.Models.IdentityResources;
using Microsoft.AspNetCore.Authentication;

namespace Tannit.AuthServer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IEmailVerificationRepository _verificationRepo;


        public AuthService(IUserRepository userRepo, PasswordService passwordService, JwtService jwtService, IEmailService emailService, IEmailVerificationRepository verificationRepo)
        {
            _userRepo = userRepo;
            _passwordService = passwordService;
            _jwtService = jwtService;
            _emailService = emailService;
            _verificationRepo = verificationRepo;
        }

        public async Task RegisterAsync(string email, string password)
        {
            var existingUser = await _userRepo.GetByEmailAsync(email);
            if (existingUser != null)
                throw new Exception("Email déjà utilisé");

            var hash = _passwordService.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hash,
                Role = "User",
                Balance = 0,
                IsEmailVerified = false 
            };

            await _userRepo.AddUserAsync(user);

            var verificationCode = new Random().Next(1000, 9999).ToString();

            var verification = new EmailVerification
            {
                Id = Guid.NewGuid(),
                Email = email,
                Code = verificationCode,
                Expiration = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            await _verificationRepo.AddAsync(verification);

            await _emailService.SendEmailAsync(
                email,
                "Vérification de votre e-mail - Tannit",
                $"Voici votre code de vérification : {verificationCode}"
            );
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);

            if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return AuthResult.Fail("Invalid credentials");

            var token = _jwtService.GenerateToken(user);
            return AuthResult.SuccessResult(token);
        }

        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);

            if (user == null)
                return null;

            return new UserProfileDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                Balance = user.Balance.ToString()
            };
        }

        public async Task<string> VerifyEmailAsync(string email, string code)
        {
            var verification = await _verificationRepo.GetByEmailAndCodeAsync(email, code) ?? throw new Exception("Code de vérification invalide");
            
            if (verification.Expiration < DateTime.UtcNow)
                throw new Exception("Code expiré");

            if (verification.IsUsed)
                throw new Exception("Code déjà utilisé");

            var user = await _userRepo.GetByEmailAsync(email) ?? throw new Exception("Utilisateur introuvable");
            
            user.IsEmailVerified = true; //a completer
            await _userRepo.UpdateUserAsync(user);

            await _verificationRepo.MarkAsUsedAsync(verification.Id);

            return _jwtService.GenerateToken(user);
        }

      

    }
}
