namespace Labaratory.Models
{
    public class AnalysisResult
    {
        public int Id { get; set; } // Идентификатор результата
        public int PatientApplicationId { get; set; } // Связь с заявкой пациента
        public PatientApplication? PatientApplication { get; set; } // Навигационное свойство

        public int AnalyzeTypeId { get; set; } // Связь с типом анализа
        public AnalyzeType? AnalyzeType { get; set; } // Навигационное свойство

        public string? Result { get; set; } // Результат анализа
        public string? NormalRange { get; set; } // Норма анализа
        public DateTime? ResultDate { get; set; } // Дата результата
    }
}
