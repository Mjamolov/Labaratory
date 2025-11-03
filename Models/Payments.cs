using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{
    public class Payments
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Application")]
        public int ApplicationId { get; set; }
        public Application? Application { get; set; } // Навигационное свойство

        public double AmountDone { get; set; }
        public DateTime AddDate { get; set; }
    }
}
