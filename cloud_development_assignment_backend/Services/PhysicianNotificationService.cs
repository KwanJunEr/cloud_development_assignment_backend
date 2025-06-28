using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;

namespace cloud_development_assignment_backend.Services
{
    public class PhysicianNotificationService
    {
        private readonly AppDbContext _context;

        public PhysicianNotificationService(AppDbContext context)
        {
            _context = context;
        }

        public void CreateNotification(int physicianId, string message, string type, string? sender = null, string? subject = null)
        {
            var notification = new PhysicianNotification
            {
                PhysicianId = physicianId,
                Message = message,
                Type = type,
                Sender = sender,
                Subject = subject,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            _context.PhysicianNotifications.Add(notification);
            _context.SaveChanges();
        }
    }
}
