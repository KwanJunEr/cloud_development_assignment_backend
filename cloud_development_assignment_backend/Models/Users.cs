using System;
using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Phone { get; set; }

        public string Specialization { get; set; }
        public string Hospital { get; set; }
        public string LicenseNumber { get; set; }
        public int? PatientId { get; set; }
        public string Relationship { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
