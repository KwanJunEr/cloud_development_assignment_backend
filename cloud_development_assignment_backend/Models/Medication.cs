using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class Medication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Dosage { get; set; }

        [Required]
        public string Frequency { get; set; }

        public string Duration { get; set; }

        public string? Notes { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("PrescriptionId")]
        public Prescription? Prescription { get; set; }
    }

}
