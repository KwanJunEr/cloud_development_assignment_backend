using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class HealthReadingOutputDto
    {
        public string Id { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double BloodSugar { get; set; }
        public double InsulinDosage { get; set; }
        public double? BodyWeight { get; set; }
        public int? SystolicBP { get; set; }
        public int? DiastolicBP { get; set; }
        public int? HeartRate { get; set; }
        public string? MealContext { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
    }
}
