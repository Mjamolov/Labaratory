namespace Labaratory.Models
{
    public class AnalyzeSubItem
    {
        public int Id { get; set; }

        public int AnalyzeTypeId { get; set; }
        public AnalyzeType? AnalyzeType { get; set; }

        public string? Name { get; set; }
        public string? Unit { get; set; }
        public string? NormalRange { get; set; }
        public string? Result { get; set; }

        public DateTime AddDate { get; set; } = DateTime.Now;
    }


    public class SubItemResult
    {
        public int Id { get; set; }

        public int PatientApplicationId { get; set; }
        public PatientApplication? PatientApplication { get; set; }

        public int AnalyzeSubItemId { get; set; }
        public AnalyzeSubItem? AnalyzeSubItem { get; set; }

        public string? Result { get; set; }
        public string? NormalRange { get; set; }
    }

}
