namespace cloud_development_assignment_backend.DTO
{
    public class PatientOutputDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string? DiabetesType { get; set; }
        public DateTime? DiagnosisDate { get; set; }
        public DateTime? LastAppointment { get; set; }
    }
}
