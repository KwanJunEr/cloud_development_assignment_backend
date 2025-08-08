using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.Models
{
    public class MedicalSupply
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FamilyId { get; set; }

        [ForeignKey(nameof(FamilyId))]
        public User Family { get; set; }

        [Required]
        public int PatientId { get; set; }

        [ForeignKey(nameof(PatientId))]
        public User Patient { get; set; }

        [Required]
        [MaxLength(255)]
        public string MedicineName { get; set; }

        [MaxLength(1000)]
        public string MedicineDescription { get; set; }

        [Required]
        public int Quantity { get; set; }

        [MaxLength(50)]
        public string Unit { get; set; }

        [MaxLength(500)]
        public string PlaceToPurchase { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [MaxLength(100)]
        public string Status { get; set; }

        [MaxLength(2000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
