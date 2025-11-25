using System.ComponentModel.DataAnnotations;

namespace UserService.Domain.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string Username { get; set; }

        [EmailAddress]
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}
