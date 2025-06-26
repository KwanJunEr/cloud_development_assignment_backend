namespace cloud_development_assignment_backend.Models
{
    public class DietTip
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
