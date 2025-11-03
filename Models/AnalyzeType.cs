using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{
    public class AnalyzeType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название анализа обязательно.")]
        public string? AnalyzeName { get; set; }

        public string? DoctorId { get; set; }
        public string? DoctorName { get; set; } 

        [ForeignKey("AnalyzeCategory")]
        public int? AnalyzeCategoryId { get; set; } 
        public AnalyzeCategory? AnalyzeCategory { get; set; }

        public double? Price { get; set; } 
        public bool Status { get; set; }

        public bool TypeAnalyzeID { get; set; }
        public string? Unit { get; set; }
        public string? TextResult { get; set; }
        public string? NormalResult { get; set; }
        public decimal? DoctorPayoutPercentage { get; set; }

        public DateTime? AddDate { get; set; } 

        [NotMapped]
        public bool IsSelected { get; set; }
    }

}