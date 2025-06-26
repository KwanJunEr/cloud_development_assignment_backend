using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class FollowUpOutputDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime FlaggedDate { get; set; }
        public string FlagReason { get; set; }
        public string FlaggedBy { get; set; }
        public string UrgencyLevel { get; set; }
        public string Status { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string FollowUpNotes { get; set; }
    }
}
