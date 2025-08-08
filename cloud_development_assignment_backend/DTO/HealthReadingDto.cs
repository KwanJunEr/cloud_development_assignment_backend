using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class HealthReadingDto
    {
        public int UserId { get; set; }

        public string Date { get; set; } = string.Empty; // e.g., "2025-06-25"

        public string Time { get; set; } = string.Empty; // e.g., "09:00"

        public double BloodSugar { get; set; }

        public double InsulinDosage { get; set; }

        public double? BodyWeight { get; set; }

        public int? SystolicBP { get; set; }

        public int? DiastolicBP { get; set; }

        public int? HeartRate { get; set; }

        public string? MealContext { get; set; }

        public string? Notes { get; set; }

        public string? ImageUrl { get; set; }
    }
}
