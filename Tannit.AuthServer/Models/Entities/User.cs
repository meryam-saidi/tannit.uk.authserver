using System.ComponentModel.DataAnnotations;

namespace Tannit.AuthServer.Models.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        //[Required]
        [EmailAddress]
        public string Email { get; set; } = default;

        //public string Username { get; set; }

        //[Required]
        public string PasswordHash { get; set; } = default;

        //[Required]
        public string Role { get; set; } = default;

        public decimal Balance { get; set; }

        public bool IsEmailVerified { get; set; } = false;
    }
}
