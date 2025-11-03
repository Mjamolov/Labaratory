using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Lastname { get; set; }
        public string? Adress { get; set; }
        public DateTime? BirthDay { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? GuidId { get; set; }

        public ICollection<Application>? Applications { get; set; }
    }
}
