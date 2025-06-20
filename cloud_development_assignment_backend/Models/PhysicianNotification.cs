namespace cloud_development_assignment_backend.Models
{
    public class PhysicianNotification
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public bool IsRead { get; set; }
        public string PhysicianId { get; set; }
    }
}
