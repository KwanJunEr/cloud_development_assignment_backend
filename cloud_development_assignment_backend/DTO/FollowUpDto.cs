using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class FollowUpDto
    {
        public int PatientId { get; set; } 
        public DateTime FlaggedDate { get; set; }
        public string FlagReason { get; set; }
        public string FlaggedBy { get; set; }
        public string UrgencyLevel { get; set; }
        public string Status { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string FollowUpNotes { get; set; }
    }
}
