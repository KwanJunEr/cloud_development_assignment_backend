namespace cloud_development_assignment_backend.DTO
{
    public class MedicalSupplyDto
    {
        public int Id { get; set; }
        public int FamilyId { get; set; }
        public int PatientId { get; set; }
        public string MedicineName { get; set; }
        public string MedicineDescription { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string? PlaceToPurchase { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}
