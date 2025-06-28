using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    [Table("DietTip")] // Explicitly maps to your table name
    public class DietTip
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DieticianId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("DieticianId")]
        public User Dietician { get; set; }
    }
}