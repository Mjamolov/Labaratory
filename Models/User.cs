using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{
    public class User : IdentityUser
    {
        public string? FirsName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        [NotMapped]
        public string? Role { get; set; }
    }
}
