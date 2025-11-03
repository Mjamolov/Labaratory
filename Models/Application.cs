using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{
    public class Application
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Pacient")]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [ForeignKey("AnalyzeType")]
        public int AnalyzeId { get; set; }
        public AnalyzeType? AnalyzeType { get; set; } // Навигационное свойство

        [ForeignKey("Payments")]
        public int PaymentId { get; set; }
        public Payments? Payment { get; set; } // Навигационное свойство

        public double TotalPrice { get; set; }
        public DateTime CretedDate { get; set; } = DateTime.Now;
        public DateTime AddDate { get; set; }
        public string? AnalyzeName { get; set; }

    }
}
