namespace cloud_development_assignment_backend.Models
{
    public class TreatmentPlan
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public DateTime Date { get; set; }
        public string Diagnosis { get; set; }
        public string TreatmentGoals { get; set; }
        public string DietaryRecommendations { get; set; }
        public string ExerciseRecommendations { get; set; }
        public string MedicationNotes { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

