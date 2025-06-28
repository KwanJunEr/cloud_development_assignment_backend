namespace cloud_development_assignment_backend.DTO
{
    public class PatientAppointmentBookingDto
    {
        public int PatientID { get; set; }
        public int ProviderID { get; set; }
        public string Role { get; set; } = null!;
        public string ProviderName { get; set; } = null!;
        public string ProviderSpecialization { get; set; } = null!;
        public string ProviderVenue { get; set; } = null!;
        public DateTime ProviderAvailableDate { get; set; }
        public string ProviderAvailableTimeSlot { get; set; } = null!;
        public string BookingMode { get; set; } = null!;
        public string ServiceBooked { get; set; } = null!;
        public string? ReasonsForVisit { get; set; }
        public string Status { get; set; } = null!;
    }
}
