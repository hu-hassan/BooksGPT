using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BooksGPT.Models
{
    [Index(nameof(Username), IsUnique = true)] // <-- Move Index attribute here
    [Index(nameof(Email), IsUnique = true)] // Make Email unique

    public class UserModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }  // Auto-increment primary key

        [Required]
        public string Username { get; set; }
        public string Password { get; set; }
        public string salt { get; set; }
        public string pattern { get; set; }
        [Required]
        public string Email { get; set; }
        // Display name for the user
        [Required]
        public string Name { get; set; }

        // Avatar color stored as hex string, e.g. "#aabbcc"
        public string AvatarColor { get; set; }
        public bool IsEmailVerified { get; set; } = false;

    }
}
