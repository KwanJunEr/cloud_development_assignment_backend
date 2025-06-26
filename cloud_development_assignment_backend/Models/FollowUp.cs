using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class FollowUp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int PatientId { get; set; }

        [Required]
        public DateTime FlaggedDate { get; set; }

        [Required]
        public string? FlagReason { get; set; }

        [Required]
        public string? FlaggedBy { get; set; }

        [Required]
        public string? UrgencyLevel { get; set; }

        public string? Status { get; set; }

        public DateTime? FollowUpDate { get; set; }

        public string? FollowUpNotes { get; set; }

        public User User { get; set; }
    }
}