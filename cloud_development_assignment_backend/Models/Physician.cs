namespace cloud_development_assignment_backend.Models
{
    public class Physician
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string LicenseNumber { get; set; }

        // Navigation properties
        public ICollection<PhysicianNotification> Notifications { get; set; } = new List<PhysicianNotification>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
