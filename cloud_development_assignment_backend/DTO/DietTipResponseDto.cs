namespace cloud_development_assignment_backend.DTO
{
    public class DietTipResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string DieticianFullName { get; set; }
        public string DieticianSpecialization { get; set; }
        public string DieticianHospital { get; set; }
    }
}
