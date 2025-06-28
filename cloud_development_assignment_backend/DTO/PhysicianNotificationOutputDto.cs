namespace cloud_development_assignment_backend.DTO
{
    public class PhysicianNotificationOutputDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string CreatedAt { get; set; }  
        public bool IsRead { get; set; }
        public int PhysicianId { get; set; }
        public string Type { get; set; }
        public string? Sender { get; set; }
        public string? Subject { get; set; }
    }
}
