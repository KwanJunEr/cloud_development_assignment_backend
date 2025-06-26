using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class WordofEncouragementDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int FamilyId { get; set; }

        [Required]
        public string Content { get; set; } = null!;
    }
