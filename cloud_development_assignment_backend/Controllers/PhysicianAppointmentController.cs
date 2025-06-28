using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Services;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhysicianAppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PhysicianNotificationService _notificationService;

        public PhysicianAppointmentController(AppDbContext context)
        {
            _context = context;
            _notificationService = new PhysicianNotificationService(_context);
        }

        // GET: api/PhysicianAppointment/{providerId}/appointments
        // GET: api/PhysicianAppointment/{providerId}/appointments?status=confirmed
        [HttpGet("{providerId}/appointments")]
        public async Task<IActionResult> GetAppointments(int providerId, [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.PatientAppointmentBooking
                    .Where(a => a.ProviderID == providerId);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(a => a.Status.ToLower() == status.ToLower());
                }

                var appointments = await query
                    .OrderByDescending(a => a.ProviderAvailableDate)
                    .Join(
                        _context.Users,
                        appt => appt.PatientID,
                        user => user.Id,
                        (appt, user) => new PhysicianAppointmentOutputDto
                        {
                            Id = appt.Id,
                            PatientID = appt.PatientID,
                            PatientName = user.FirstName + " " + user.LastName,
                            ProviderAvailableDate = appt.ProviderAvailableDate,
                            ProviderAvailableTimeSlot = appt.ProviderAvailableTimeSlot,
                            Status = appt.Status,
                            Role = appt.Role,
                            ProviderName = appt.ProviderName,
                            ProviderSpecialization = appt.ProviderSpecialization,
                            ProviderVenue = appt.ProviderVenue,
                            BookingMode = appt.BookingMode,
                            ServiceBooked = appt.ServiceBooked,
                            ReasonsForVisit = appt.ReasonsForVisit
                        }
                    )
                    .ToListAsync();

                return Ok(new{appointments});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving provider appointments.",
                    error = ex.Message
                });
            }
        }

        

        // GET: api/PhysicianAppointment/{providerId}/appointments/{appointmentId}
        [HttpGet("{providerId}/appointments/{appointmentId}")]
        public async Task<IActionResult> GetAppointmentById(int providerId, int appointmentId)
        {
            var appointment = await _context.PatientAppointmentBooking
                .FirstOrDefaultAsync(a => a.ProviderID == providerId && a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found for this provider." });
            }

            return Ok(appointment);
        }

        // PUT: api/PhysicianAppointment/{providerId}/appointments/{appointmentId}/status
        [HttpPut("{providerId}/appointments/{appointmentId}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int providerId, int appointmentId, [FromBody] string status)
        {
            var appointment = await _context.PatientAppointmentBooking
                .FirstOrDefaultAsync(a => a.ProviderID == providerId && a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found for this provider." });
            }

            if (string.IsNullOrEmpty(status))
            {
                return BadRequest(new { message = "Status is required." });
            }

            appointment.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment status updated successfully.", appointment });
        }

        // GET: api/PhysicianAppointment/{providerId}/appointments/status/{status}
        [HttpGet("{providerId}/appointments/status/{status}")]
        public async Task<IActionResult> GetAppointmentsByStatus(int providerId, string status)
        {
            try
            {
                var appointments = await _context.PatientAppointmentBooking
                    .Where(a => a.ProviderID == providerId && a.Status.ToLower() == status.ToLower())
                    .OrderByDescending(a => a.ProviderAvailableDate)
                    .Join(
                        _context.Users,
                        appt => appt.PatientID,
                        user => user.Id,
                        (appt, user) => new PhysicianAppointmentOutputDto
                        {
                            Id = appt.Id,
                            PatientID = appt.PatientID,
                            PatientName = user.FirstName + " " + user.LastName,
                            ProviderAvailableDate = appt.ProviderAvailableDate,
                            ProviderAvailableTimeSlot = appt.ProviderAvailableTimeSlot,
                            Status = appt.Status,
                            Role = appt.Role,
                            ProviderName = appt.ProviderName,
                            ProviderSpecialization = appt.ProviderSpecialization,
                            ProviderVenue = appt.ProviderVenue,
                            BookingMode = appt.BookingMode,
                            ServiceBooked = appt.ServiceBooked,
                            ReasonsForVisit = appt.ReasonsForVisit
                        }
                    )
                    .ToListAsync();

                return Ok(new { appointments });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while filtering appointments.",
                    error = ex.Message
                });
            }
        }

        // GET: api/PhysicianAppointment/{providerId}/appointments/upcoming
        [HttpGet("{providerId}/appointments/upcoming")]
        public async Task<IActionResult> GetUpcomingAppointments(int providerId)
        {
            var today = DateTime.Today;
            var appointments = await _context.PatientAppointmentBooking
                .Where(a => a.ProviderID == providerId
                            && a.Status.ToLower() == "confirmed"
                            && a.ProviderAvailableDate >= today)
                .OrderBy(a => a.ProviderAvailableDate)
                .Join(
                    _context.Users,
                    appt => appt.PatientID,
                    user => user.Id,
                    (appt, user) => new PhysicianAppointmentOutputDto
                    {
                        Id = appt.Id,
                        PatientID = appt.PatientID,
                        PatientName = user.FirstName + " " + user.LastName,
                        ProviderAvailableDate = appt.ProviderAvailableDate,
                        ProviderAvailableTimeSlot = appt.ProviderAvailableTimeSlot,
                        Status = appt.Status,
                        Role = appt.Role,
                        ProviderName = appt.ProviderName,
                        ProviderSpecialization = appt.ProviderSpecialization,
                        ProviderVenue = appt.ProviderVenue,
                        BookingMode = appt.BookingMode,
                        ServiceBooked = appt.ServiceBooked,
                        ReasonsForVisit = appt.ReasonsForVisit
                    }
                )
                .ToListAsync();

            return Ok(new { appointments });
        }
    }
}
