using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhysicianDashboardController : ControllerBase
    {
        private readonly ILogger<PhysicianDashboardController> _logger;
        // KCG: use repository or service here
        public static readonly List<Appointment> _appointments = new List<Appointment>();
        public static readonly List<FollowUp> _followUps = new List<FollowUp>();
        public static readonly List<HealthLog> _healthLogs = new List<HealthLog>();
        public static readonly List<PhysicianNotification> _notifications = new List<PhysicianNotification>();

        // KCG: Default physician ID for demo purposes
        private const string DEFAULT_PHYSICIAN_ID = "physician-001";

        public PhysicianDashboardController(ILogger<PhysicianDashboardController> logger)
        {
            _logger = logger;

            // KCG: Initialize with sample data if empty
            InitializeData();
        }

        private void InitializeData()
        {
            // Initialize appointments if empty
            // KCG: remove when real data
            if (!_appointments.Any())
            {
                _appointments.Add(new Appointment
                {
                    Id = "apt1",
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    PatientId = "p1",
                    Date = DateTime.Today.AddDays(1),
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(9, 30, 0),
                    Status = "booked",
                    Notes = "Initial consultation"
                });

                _appointments.Add(new Appointment
                {
                    Id = "apt2",
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    PatientId = "p2",
                    Date = DateTime.Today.AddDays(2),
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Status = "booked",
                    Notes = "Follow-up appointment"
                });

                _appointments.Add(new Appointment
                {
                    Id = "apt3",
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    PatientId = "p3",
                    Date = DateTime.Today.AddDays(3),
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(14, 30, 0),
                    Status = "booked",
                    Notes = "Medication review"
                });
            }

            // Initialize notifications if empty
            // KCG: remove when real data   
            if (!_notifications.Any())
            {
                _notifications.Add(new PhysicianNotification
                {
                    Id = 1,
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    Message = "Patient John Doe needs urgent follow-up",
                    Time = DateTime.Now.AddMinutes(-10),
                    IsRead = false
                });

                _notifications.Add(new PhysicianNotification
                {
                    Id = 2,
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    Message = "New health log from Mary Smith shows concerning vitals",
                    Time = DateTime.Now.AddHours(-1),
                    IsRead = false
                });

                _notifications.Add(new PhysicianNotification
                {
                    Id = 3,
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    Message = "Appointment with James Wilson confirmed for tomorrow",
                    Time = DateTime.Now.AddHours(-3),
                    IsRead = true
                });

                _notifications.Add(new PhysicianNotification
                {
                    Id = 4,
                    PhysicianId = DEFAULT_PHYSICIAN_ID,
                    Message = "Lab results available for patient #12345",
                    Time = DateTime.Now.AddDays(-1),
                    IsRead = true
                });
            }
        }

        // GET: api/PhysicianDashboard/5
        [HttpGet("{physicianId}")]
        public ActionResult<PhysicianDashboard> GetDashboardData(string physicianId)
        {
            if (string.IsNullOrEmpty(physicianId))
            {
                return BadRequest("Physician ID is required");
            }

            // Get upcoming appointments
            var upcomingAppointments = _appointments
                .Count(a => a.PhysicianId == physicianId &&
                           a.Status == "booked" &&
                           a.Date >= DateTime.Today);

            // Get urgent follow-ups
            var urgentFollowUps = _followUps
                .Count(f => f.UrgencyLevel == "high" &&
                           f.Status != "resolved");

            // Get recent health logs (last 24 hours)
            var recentLogs = _healthLogs
                .Count(h => h.CreatedAt >= DateTime.UtcNow.AddHours(-24));

            // Get unread messages/notifications
            var newMessages = _notifications
                .Count(n => n.PhysicianId == physicianId &&
                           !n.IsRead);

            var dashboard = new PhysicianDashboard
            {
                UpcomingAppointments = upcomingAppointments,
                UrgentFollowups = urgentFollowUps,
                RecentLogs = recentLogs,
                NewMessages = newMessages
            };

            return Ok(dashboard);
        }

        // GET: api/PhysicianDashboard/5/notifications
        [HttpGet("{physicianId}/notifications")]
        public ActionResult<IEnumerable<PhysicianNotification>> GetNotifications(string physicianId)
        {
            if (string.IsNullOrEmpty(physicianId))
            {
                return BadRequest("Physician ID is required");
            }

            var notifications = _notifications
                .Where(n => n.PhysicianId == physicianId)
                .OrderByDescending(n => n.Time)
                .ToList();

            return Ok(notifications);
        }

        // PUT: api/PhysicianDashboard/notifications/5/read
        [HttpPut("notifications/{notificationId}/read")]
        public IActionResult MarkNotificationAsRead(int notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;

            return NoContent();
        }

        // POST: api/PhysicianDashboard/notifications
        [HttpPost("notifications")]
        public ActionResult<PhysicianNotification> CreateNotification(PhysicianNotification notification)
        {
            if (notification == null)
            {
                return BadRequest();
            }

            // Validate required fields
            if (string.IsNullOrEmpty(notification.PhysicianId) ||
                string.IsNullOrEmpty(notification.Message))
            {
                return BadRequest("PhysicianId and Message are required fields");
            }

            // Generate an ID if not provided
            if (notification.Id <= 0)
            {
                // In a real app, this would be handled by the database
                // KCG: just for demo
                notification.Id = _notifications.Count > 0 ? _notifications.Max(n => n.Id) + 1 : 1;
            }

            // Set defaults if not provided
            if (notification.Time == default)
            {
                notification.Time = DateTime.UtcNow;
            }

            notification.IsRead = false;

            _notifications.Add(notification);

            return CreatedAtAction(nameof(GetNotifications),
                new { physicianId = notification.PhysicianId }, notification);
        }
    }
}
