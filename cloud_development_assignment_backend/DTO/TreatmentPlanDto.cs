using System;

namespace cloud_development_assignment_backend.DTO
{
    public class TreatmentPlanDto
    {
        public int PatientId { get; set; }
        public DateTime Date { get; set; }
        public string Diagnosis { get; set; }
        public string TreatmentGoals { get; set; }
        public string? DietaryRecommendations { get; set; }
        public string? ExerciseRecommendations { get; set; }
        public string? MedicationNotes { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
