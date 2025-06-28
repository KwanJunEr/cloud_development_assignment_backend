using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    [Table("PatientAppointmentBooking")]
    public class PatientAppointmentBooking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        public int ProviderID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Role { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string ProviderName { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string ProviderSpecialization { get; set; } = null!;

        [Required]
        [MaxLength(800)]
        public string ProviderVenue { get; set; } = null!;

        [Required]
        public DateTime ProviderAvailableDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProviderAvailableTimeSlot { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string BookingMode { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ServiceBooked { get; set; } = null!;

        [MaxLength(500)]
        public string? ReasonsForVisit { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!;

        [ForeignKey(nameof(PatientID))]
        public User Patient { get; set; } = null!;

        [ForeignKey(nameof(ProviderID))]
        public User Provider { get; set; } = null!;
    }
}
