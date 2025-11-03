using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Labaratory.Models
{
    public class AnalyzeCategory
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название категории обязательно.")]
        [StringLength(100, ErrorMessage = "Название категории должно содержать не более 100 символов.")]
        public string CategoryName { get; set; } = string.Empty;

        public DateTime AddDate { get; set; } = DateTime.UtcNow;

        public ICollection<AnalyzeType> AnalyzeTypes { get; set; } = new List<AnalyzeType>();
    }
}
