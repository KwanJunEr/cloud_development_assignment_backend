using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class MealEntryDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        [RegularExpression("^(breakfast|lunch|dinner)$", ErrorMessage = "MealType must be 'breakfast', 'lunch', or 'dinner'.")]
        public string MealType { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FoodItem { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Portion { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
