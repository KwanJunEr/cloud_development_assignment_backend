using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class WordsofEncouragement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int FamilyId { get; set; }

        [Required]
        public DateTime MessageDate { get; set; }

        [Required]
        public TimeSpan MessageTime { get; set; }

        [Required]
        public string Content { get; set; } = null;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PatientId")]
        public User? Patient { get; set; }

        [ForeignKey("FamilyId")]
        public User? Family { get; set; }

    }
}
