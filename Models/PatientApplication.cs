using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{

    public class PatientApplication
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
        public string? SelectedDoctors { get; set; }
        public string? SelectedAnalyzeTypes { get; set; }
        public string? PaymentType { get; set; }
        public double? PaymentAmount { get; set; }
        public int? Discount { get; set; }
        public double TotalCost { get; set; }
        public double FinalCost { get; set; }
        public bool IsFullyPaid { get; set; }
        public DateTime AddDate { get; set; } = DateTime.Now;
        public string? UniqId { get; set; }

        public List<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
        public List<Prescription> Prescriptions { get; set; } = new List<Prescription>();

        [NotMapped] public List<User> DoctorsList { get; set; } = new();
        [NotMapped] public List<string> AnalyzeNames { get; set; } = new();
        [NotMapped] public List<AnalyzeType> AnalyzeTypesList { get; set; } = new();
    }
}
