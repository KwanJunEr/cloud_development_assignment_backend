using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class ProviderAvailabilityDto
    {
        public int ProviderId { get; set; }

        public DateTime AvailabilityDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string? Notes { get; set; }
    }
}
