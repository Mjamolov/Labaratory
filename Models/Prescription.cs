namespace Labaratory.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        public int PatientApplicationId { get; set; }
        public PatientApplication? PatientApplication { get; set; }

        public string? Text { get; set; } = string.Empty;

        public DateTime AddDate { get; set; } = DateTime.Now;
    }
}
