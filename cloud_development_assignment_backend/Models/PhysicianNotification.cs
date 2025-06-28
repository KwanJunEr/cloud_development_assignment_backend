using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace cloud_development_assignment_backend.Models
{
    public class PhysicianNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsRead { get; set; } = false;

        [Required]
        public int PhysicianId { get; set; }


        public string Type { get; set; } // 'patient', 'system', 'appointment', 'followup'

        public string? Sender { get; set; }

        public string? Subject { get; set; }
    }
}
