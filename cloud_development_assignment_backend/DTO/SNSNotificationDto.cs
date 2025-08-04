using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cloud_development_assignment_backend.DTO
{
    public class SNSNotificationDto
    {
        public int NotificationId { get; set; }
        public int PhysicianId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Sender { get; set; }
        public string? Subject { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
