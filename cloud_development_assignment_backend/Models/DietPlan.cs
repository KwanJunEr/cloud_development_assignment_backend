using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class DietPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DieticianId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string MealType { get; set; }

        [Required]
        public string MealPlan { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("DieticianId")]
        public User Dietician { get; set; }

        [ForeignKey("PatientId")]
        public User Patient { get; set; }
    }
}
