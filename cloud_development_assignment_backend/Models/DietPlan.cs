namespace cloud_development_assignment_backend.Models
{
    public class DietPlan
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
