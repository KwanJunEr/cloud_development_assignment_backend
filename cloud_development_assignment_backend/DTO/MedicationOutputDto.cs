namespace cloud_development_assignment_backend.DTO
{

    public class MedicationOutputDto
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public string Name { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Duration { get; set; }
        public string? Notes { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
