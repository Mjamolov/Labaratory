using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Labaratory.Models
{
    public class Statistics
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Application")]
        public int? AppNum { get; set; } 

        [ForeignKey("Patient")]
        public int? PatientId { get; set; } 

        [Required]
        [MaxLength(255)]
        public string ExpenseName { get; set; } 

        [Required]
        public decimal Amount { get; set; } 

        [Required]
        public DateTime AddDate { get; set; } 
    }
}
