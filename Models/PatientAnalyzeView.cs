namespace Labaratory.Models
{
    public class PatientAnalyzeView
    {
        public Patient? Patient { get; set; }
        public List<AnalyzeType>? AnalyzeType { get; set; }
        public List<User>? Doctor { get; set; }
        public string? PaymentType { get; set; }
        public double? PaymentAmount { get; set; }
        public int? Discount { get; set; }
        //public List<AnalyzeCategory>? CategoriesAnalyze { get; set; }
    }
}
