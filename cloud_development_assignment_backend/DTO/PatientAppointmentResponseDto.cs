namespace cloud_development_assignment_backend.DTO
{
    public class PatientAppointmentResponseDto
    {
        public int Id { get; set; }
        public int PatientID { get; set; }
        public int ProviderID { get; set; }
        public string Role { get; set; }
        public string ProviderName { get; set; }
        public string ProviderSpecialization { get; set; }
        public string ProviderVenue { get; set; }
        public DateTime ProviderAvailableDate { get; set; }
        public string ProviderAvailableTimeSlot { get; set; }
        public string BookingMode { get; set; }
        public string ServiceBooked { get; set; }
        public string? ReasonsForVisit { get; set; }
        public string Status { get; set; }

        public string PatientFullName { get; set; }
    }
}
