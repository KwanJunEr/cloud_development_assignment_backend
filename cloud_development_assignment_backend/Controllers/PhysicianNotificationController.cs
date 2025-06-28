using Microsoft.AspNetCore.Mvc;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Data;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhysicianNotificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PhysicianNotificationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PhysicianNotification/{physicianId}
        [HttpGet("{physicianId}")]
        public ActionResult<IEnumerable<PhysicianNotificationOutputDto>> GetAll(int physicianId, [FromQuery] string? type = null, [FromQuery] bool? unread = null)
        {
            var query = _context.PhysicianNotifications.Where(n => n.PhysicianId == physicianId);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(n => n.Type == type);

            if (unread.HasValue)
                query = query.Where(n => n.IsRead == !unread.Value);


            var result = query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new PhysicianNotificationOutputDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt.ToString("o"),
                    IsRead = n.IsRead,
                    PhysicianId = n.PhysicianId,
                    Type = n.Type,
                    Sender = n.Sender,
                    Subject = n.Subject
                })
                .ToList();

            return Ok(result);
        }

        // GET: api/PhysicianNotification/{physicianId}/unread
        [HttpGet("{physicianId}/unread")]
        public ActionResult<IEnumerable<PhysicianNotificationOutputDto>> GetUnread(int physicianId)
        {
            return GetAll(physicianId, unread: true);
        }

        // GET: api/PhysicianNotification/{physicianId}/type/{type}
        [HttpGet("{physicianId}/type/{type}")]
        public ActionResult<IEnumerable<PhysicianNotificationOutputDto>> GetByType(int physicianId, string type)
        {
            return GetAll(physicianId, type: type);
        }

        // POST: api/PhysicianNotification
        [HttpPost]
        public ActionResult<PhysicianNotificationOutputDto> Create([FromBody] PhysicianNotification notification)
        {
            if (notification == null)
                return BadRequest();

            if (notification.PhysicianId == 0 ||
                string.IsNullOrEmpty(notification.Message) ||
                string.IsNullOrEmpty(notification.Type))
            {
                return BadRequest("PhysicianId, Message, and Type are required fields");
            }

            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;

            _context.PhysicianNotifications.Add(notification);
            _context.SaveChanges();

            var dto = new PhysicianNotificationOutputDto
            {
                Id = notification.Id,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt.ToString("o"),
                IsRead = notification.IsRead,
                PhysicianId = notification.PhysicianId,
                Type = notification.Type,
                Sender = notification.Sender,
                Subject = notification.Subject
            };

            return CreatedAtAction(nameof(GetAll), new { physicianId = notification.PhysicianId }, dto);
        }

        // PUT: api/PhysicianNotification/{id}/read
        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var notification = _context.PhysicianNotifications.FirstOrDefault(n => n.Id == id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            _context.SaveChanges();
            return NoContent();
        }

        // DELETE: api/PhysicianNotification/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var notification = _context.PhysicianNotifications.FirstOrDefault(n => n.Id == id);
            if (notification == null)
                return NotFound();

            _context.PhysicianNotifications.Remove(notification);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
