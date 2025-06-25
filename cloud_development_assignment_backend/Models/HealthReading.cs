using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class HealthReading
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public int UserId { get; set; }

        // This is already inferred, but you can keep it for clarity
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;  //User is the navigational property  navigate from one entity (class) to its related entity (another class/table)

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public float BloodSugar { get; set; }

        [Required]
        public float InsulinDosage { get; set; }

        public float? BodyWeight { get; set; }

        public int? SystolicBP { get; set; }

        public int? DiastolicBP { get; set; }

        public int? HeartRate { get; set; }

        public string? MealContext { get; set; }

        public string? Notes { get; set; }

        public string? Status { get; set; }
    }
}
