namespace cloud_development_assignment_backend.DTO
{
    public class PatientFamilyDto
    {
        public int FamilyId { get; set; }
        public int? PatientId { get; set; }
        public string? Relationship { get; set; }
    }
}
