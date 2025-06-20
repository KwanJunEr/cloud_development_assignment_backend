namespace cloud_development_assignment_backend.Models
{
    public class Appointment
    {
        public string Id { get; set; }
        public string PhysicianId { get; set; }
        public string PatientId { get; set; } 
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } // "available", "booked", "unavailable"
        public string Notes { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Physician Physician { get; set; }
        public Patient Patient { get; set; }
    }
}
