using System;

namespace cloud_development_assignment_backend.Models
{
    public class FollowUp
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public DateTime FlaggedDate { get; set; }
        public string FlagReason { get; set; }
        public string FlaggedBy { get; set; }
        public string UrgencyLevel { get; set; }
        public string Status { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public string FollowUpNotes { get; set; }

        // Navigation property for Patient
        public Patient Patient { get; set; }
    }
}
