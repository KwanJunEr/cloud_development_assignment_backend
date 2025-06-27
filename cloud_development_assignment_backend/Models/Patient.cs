using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace cloud_development_assignment_backend.Models
{
    public class Patient
    {
        [Key]
        [ForeignKey("User")]
        public int PatientId { get; set; }
        public string? DiabetesType { get; set; }
        public DateTime? DiagnosisDate { get; set; }
        public DateTime? LastAppointment { get; set; }

        public User? User { get; set; }
    }
}

