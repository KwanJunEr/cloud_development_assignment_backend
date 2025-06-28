using cloud_development_assignment_backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Models;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientAppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientAppointmentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dietician")]
        public async Task<IActionResult> GetDieticians()
        {
            var dieticians = await _context.Users
                .Where(u => u.Role.ToLower() == "dietician")
                .ToListAsync();

            return Ok(new
            {
                message = "Sucessfully retrived all dieticiains",
                dieticians
            });
        }

        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors()
        {
            var doctors = await _context.Users
                .Where(u => u.Role.ToLower() == "doctor")
                .ToListAsync();

            return Ok(new
            {
                message = "Sucessfully retrived all doctors",
                doctors
            });
        }

        [HttpGet("doctor-availability/{providerId}")]
        public async Task<IActionResult> GetProviderAvailability(int providerId)
        {
            try
            {
                var availability = await _context.ProviderAvailabilities
                     .Where(a =>
                        a.ProviderId == providerId &&
                        a.AvailabilityDate >= DateTime.Today &&
                        a.Status == "Available"
                        )
                        .OrderBy(a => a.AvailabilityDate)
                        .Select(a => new
                        {
                            date = a.AvailabilityDate.ToString("yyyy-MM-dd"),
                            timeRange = $"{a.StartTime:hh\\:mm} - {a.EndTime:hh\\:mm}",
                            notes = a.Notes
                        })
                        .ToListAsync();
                return Ok(new
                {
                    message = "Successfully retrieved available provider slots",
                    availability
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving provider availability",
                    error = ex.Message
                });

            }
        }

        [HttpGet("dietician-availability/{providerId}")]
        public async Task<IActionResult> GetDieticianAvailability(int providerId)
        {
            try
            {
                var availability = await _context.ProviderAvailabilities
                     .Where(a =>
                        a.ProviderId == providerId &&
                        a.AvailabilityDate >= DateTime.Today &&
                        a.Status == "Available"
                        )
                        .OrderBy(a => a.AvailabilityDate)
                        .Select(a => new
                        {
                            date = a.AvailabilityDate.ToString("yyyy-MM-dd"),
                            timeRange = $"{a.StartTime:hh\\:mm} - {a.EndTime:hh\\:mm}",
                            notes = a.Notes
                        })
                        .ToListAsync();
                return Ok(new
                {
                    message = "Successfully retrieved available provider slots",
                    availability
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving provider availability",
                    error = ex.Message
                });

            }
        }

        [HttpPost("create")] 
        public async Task<IActionResult> CreateAppointment([FromBody] PatientAppointmentBookingDto dto)
        {
            try
            {
                //split and parse the time range 
                var timeParts = dto.ProviderAvailableTimeSlot.Split('-');
                if (timeParts.Length != 2)
                {
                    return BadRequest(new { error = "Invalid time slot format. Expected format: 'HH:mm - HH:mm'." });
                }

                if (!TimeSpan.TryParse(timeParts[0].Trim(), out var startTime))
                {
                    return BadRequest(new { error = "Invalid start time format." });
                }

                if (!TimeSpan.TryParse(timeParts[1].Trim(), out var endTime))
                {
                    return BadRequest(new { error = "Invalid end time format." });
                }

                // Check provider availability for this date and time slot
                var providerAvailability = await _context.ProviderAvailabilities
                    .FirstOrDefaultAsync(p =>
                        p.ProviderId == dto.ProviderID &&
                        p.AvailabilityDate == dto.ProviderAvailableDate &&
                        p.StartTime == startTime &&
                        p.EndTime == endTime);

                if (providerAvailability == null)
                {
                    return BadRequest(new { error = "Provider availability not found for the selected date and time slot." });
                }

                if (providerAvailability.Status.Equals("taken", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "The selected time slot is already taken." });
                }

                // All good, proceed with creating appointment
                var appointment = new PatientAppointmentBooking
                {
                    PatientID = dto.PatientID,
                    ProviderID = dto.ProviderID,
                    Role = dto.Role,
                    ProviderName = dto.ProviderName,
                    ProviderSpecialization = dto.ProviderSpecialization,
                    ProviderVenue = dto.ProviderVenue,
                    ProviderAvailableDate = dto.ProviderAvailableDate,
                    ProviderAvailableTimeSlot = dto.ProviderAvailableTimeSlot,
                    BookingMode = dto.BookingMode,
                    ServiceBooked = dto.ServiceBooked,
                    ReasonsForVisit = dto.ReasonsForVisit,
                    Status = dto.Status
                };

                _context.PatientAppointmentBooking.Add(appointment);

                // Update availability status to "taken"
                providerAvailability.Status = "taken";

                await _context.SaveChangesAsync();

                return Ok(appointment);

            }
            catch(Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
