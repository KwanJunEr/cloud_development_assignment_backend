using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string? Notes { get; set; }

        public int PhysicianId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }


        public List<Medication> Medications { get; set; } = new List<Medication>();

        [ForeignKey("PatientId")]
        public User? Patient { get; set; }

        [ForeignKey("PhysicianId")]
        public User? Physician { get; set; }
    }
}