using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class TreatmentPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int PatientId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Diagnosis { get; set; }

        [Required]
        public string TreatmentGoals { get; set; }

        public string? DietaryRecommendations { get; set; }

        public string? ExerciseRecommendations { get; set; }

        public string? MedicationNotes { get; set; }

        public DateTime FollowUpDate { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; }
    }
}

