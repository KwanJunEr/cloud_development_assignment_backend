using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class MedicationReminder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(700)]
        public string MedicationName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Dosage { get; set; }

        [Required]
        public DateTime ReminderDate { get; set; }

        [Required]
        public TimeSpan ReminderDue { get; set; }

        public string? Notes { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
