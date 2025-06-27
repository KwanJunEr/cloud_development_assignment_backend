namespace cloud_development_assignment_backend.DTO
{
    public class PatientDto
    {
        public int PatientId { get; set; }
        public string? DiabetesType { get; set; }
        public DateTime? DiagnosisDate { get; set; }

        public DateTime? LastAppointment { get; set; }
    }
}
