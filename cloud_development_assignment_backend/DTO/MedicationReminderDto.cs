namespace cloud_development_assignment_backend.DTO
{
    public class MedicationReminderDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string MedicationName { get; set; }
        public string? Description { get; set; }
        public string? Dosage { get; set; }
        public DateTime ReminderDate { get; set; }
        public TimeSpan ReminderDue { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; }
    }
}
