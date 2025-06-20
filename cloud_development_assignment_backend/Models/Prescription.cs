// In Prescription.cs
using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.Models
{
    public class Prescription
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string PatientId { get; set; }

        public DateTime Date { get; set; }

        public string Notes { get; set; }

        public string PhysicianId { get; set; }

        public string PhysicianName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property 
        public List<Medication> Medications { get; set; } = new List<Medication>();
    }
}
