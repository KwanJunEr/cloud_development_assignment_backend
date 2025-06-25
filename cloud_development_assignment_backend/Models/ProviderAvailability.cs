using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace cloud_development_assignment_backend.Models
{
    [Table("ProviderAvailability")]
    public class ProviderAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required]
        public DateTime AvailabilityDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ProviderId))]
        public User User { get; set; } = null!;// Navigation property
    }
}
