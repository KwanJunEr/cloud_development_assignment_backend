using System;

namespace cloud_development_assignment_backend.Models
{
    public class Patient
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string DiabetesType { get; set; }
        public DateTime DiagnosisDate { get; set; }
        public double? LatestA1c { get; set; }
        public string EmergencyContact { get; set; }
        public string Notes { get; set; }

        // Navigation properties
        public List<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
        public List<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public List<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public List<HealthLog> HealthLogs { get; set; } = new List<HealthLog>();

        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
