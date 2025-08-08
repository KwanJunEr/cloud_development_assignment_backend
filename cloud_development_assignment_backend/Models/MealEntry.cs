using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cloud_development_assignment_backend.Models
{
    public class MealEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string MealType { get; set; } = string.Empty; // "breakfast", "lunch", "dinner"

        [Required]
        [MaxLength(255)]
        public string FoodItem { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Portion { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string? ImageUrl { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

    }
}
