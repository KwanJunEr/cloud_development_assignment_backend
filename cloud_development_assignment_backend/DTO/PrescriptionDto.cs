namespace cloud_development_assignment_backend.DTO
{
    public class PrescriptionDto
    {
        public int PatientId { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public int PhysicianId { get; set; }
        public List<MedicationDto> Medications { get; set; } = new();
    }

}
