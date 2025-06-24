using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class UserRequest
    {
        [Required]
        public string Role { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Phone { get; set; }

        public string Specialization { get; set; }
        public string Hospital { get; set; }
        public string LicenseNumber { get; set; }
        public int? PatientId { get; set; }
        public string Relationship { get; set; }
    }
}
