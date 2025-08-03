using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class CreateNotificationDto
    {
        [Required]
        public int PhysicianId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public string? Sender { get; set; }

        public string? Subject { get; set; }
    }
}
