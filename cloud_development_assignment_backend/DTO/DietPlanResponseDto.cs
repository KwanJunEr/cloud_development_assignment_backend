namespace cloud_development_assignment_backend.DTO
{
    public class DietPlanResponseDto
    {

        public int Id { get; set; }
        public int DieticianId { get; set; }
        public int PatientId { get; set; }
        public string MealType { get; set; }
        public string MealPlan { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string DieticianName { get; set; }
        public string DieticianSpecialization { get; set; }
        public string DieticianHospital { get; set; }
    }
}
