namespace cloud_development_assignment_backend.DTO
{
    public class DietPlanCreateDto
    {
        public int DieticianId { get; set; }
        public int PatientId { get; set; }
        public string MealType { get; set; }
        public string MealPlan { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
