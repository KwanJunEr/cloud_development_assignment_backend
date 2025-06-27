namespace cloud_development_assignment_backend.DTO
{
    public class PrescriptionOutputDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public int PhysicianId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<MedicationOutputDto> Medications { get; set; } = new();
    }
}
