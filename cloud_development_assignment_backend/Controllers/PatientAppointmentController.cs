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
    }
}
