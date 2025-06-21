using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly ILogger<AppointmentController> _logger;
        // KCG: In a real app, use repository or service here
        private static readonly List<Appointment> _appointments = new List<Appointment>();

        public AppointmentController(ILogger<AppointmentController> logger)
        {
            _logger = logger;
        }

        // GET: api/appointment
        [HttpGet]
        public ActionResult<IEnumerable<Appointment>> GetAllAppointments()
        {
            return Ok(_appointments);
        }

        // GET: api/appointment/5
        [HttpGet("{id}")]
        public ActionResult<Appointment> GetAppointmentById(string id)
        {
            var appointment = _appointments.FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return Ok(appointment);
        }

        // GET: api/appointment/available
        [HttpGet("available")]
        public ActionResult<IEnumerable<Appointment>> GetAvailableTimeSlots()
        {
            var availableSlots = _appointments
                .Where(a => a.Status == "available")
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .ToList();

            // Convert to frontend format
            var result = availableSlots.Select(a => new
            {
                id = a.Id,
                day = a.Date.DayOfWeek.ToString(),
                startTime = a.StartTime.ToString(@"hh\:mm"),
                endTime = a.EndTime.ToString(@"hh\:mm"),
                isAvailable = a.Status == "available"
            });

            return Ok(result);
        }

        // GET: api/appointment/physician/5
        [HttpGet("physician/{physicianId}")]
        public ActionResult<IEnumerable<Appointment>> GetAppointmentsByPhysicianId(string physicianId)
        {
            if (string.IsNullOrEmpty(physicianId))
            {
                return BadRequest("Physician ID is required");
            }

            var appointments = _appointments
                .Where(a => a.PhysicianId == physicianId)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .ToList();

            return Ok(appointments);
        }

        // GET: api/appointment/patient/5
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<Appointment>> GetAppointmentsByPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return BadRequest("Patient ID is required");
            }

            var appointments = _appointments
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .ToList();

            return Ok(appointments);
        }

        // POST: api/appointment/availability
        [HttpPost("availability")]
        public ActionResult<Appointment> AddAvailabilitySlot([FromBody] AvailabilitySlotRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request is null");
            }

            if (string.IsNullOrEmpty(request.Day) ||
                string.IsNullOrEmpty(request.StartTime) ||
                string.IsNullOrEmpty(request.EndTime))
            {
                return BadRequest("Day, StartTime, and EndTime are required fields");
            }

            if (!Enum.TryParse<DayOfWeek>(request.Day, out var dayOfWeek))
            {
                return BadRequest("Invalid day format. Use Monday, Tuesday, etc.");
            }

            var today = DateTime.Today;
            int daysUntilNextOccurrence = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilNextOccurrence == 0) daysUntilNextOccurrence = 7; // If today, use next week
            var nextOccurrence = today.AddDays(daysUntilNextOccurrence);

            if (!TimeSpan.TryParse(request.StartTime, out var startTime) ||
                !TimeSpan.TryParse(request.EndTime, out var endTime))
            {
                return BadRequest("Invalid time format. Use HH:MM format.");
            }

            // Create the appointment
            var appointment = new Appointment
            {
                Id = Guid.NewGuid().ToString(),
                PhysicianId = request.PhysicianId ?? "default-physician", // Use a default or get from authenticated user
                Date = nextOccurrence,
                StartTime = startTime,
                EndTime = endTime,
                Status = "available", 
                CreatedAt = DateTime.UtcNow
            };

            _appointments.Add(appointment);

            var result = new
            {
                id = appointment.Id,
                day = appointment.Date.DayOfWeek.ToString(),
                startTime = appointment.StartTime.ToString(@"hh\:mm"),
                endTime = appointment.EndTime.ToString(@"hh\:mm"),
                isAvailable = true
            };

            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, result);
        }

        // PUT: api/appointment/5/status
        [HttpPut("{id}/status")]
        public IActionResult UpdateTimeSlotStatus(string id, [FromBody] AppointmentStatusUpdate statusUpdate)
        {
            if (statusUpdate == null || string.IsNullOrEmpty(statusUpdate.Status))
            {
                return BadRequest("Status is required");
            }

            var appointment = _appointments.FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = statusUpdate.Status;
            appointment.UpdatedAt = DateTime.UtcNow;

            // If status is "booked", update patient ID
            if (statusUpdate.Status == "booked" && !string.IsNullOrEmpty(statusUpdate.PatientId))
            {
                appointment.PatientId = statusUpdate.PatientId;
            }

            return NoContent();
        }

        // DELETE: api/appointment/5
        [HttpDelete("{id}")]
        public IActionResult DeleteTimeSlot(string id)
        {
            var appointment = _appointments.FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            _appointments.Remove(appointment);

            return NoContent();
        }
    }

    // Helper class for updating appointment status
    public class AppointmentStatusUpdate
    {
        public string Status { get; set; }
        public string PatientId { get; set; }
    }

    // Helper class for frontend availability requests
    public class AvailabilitySlotRequest
    {
        public string Day { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string PhysicianId { get; set; }
    }
}
