namespace cloud_development_assignment_backend.Models
{
    public class DietAppointment
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
