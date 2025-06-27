using Microsoft.AspNetCore.Mvc;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Business;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthReadingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HealhtLogBusinessLogic _service;
        public HealthReadingsController(AppDbContext context, HealhtLogBusinessLogic service)
        {
            _context = context;
            _service = service;
        }

        [HttpPost]
        public IActionResult CreateReading([FromBody] HealthReadingDto dto)
        {
            try
            {
                var reading = new HealthReading
                {
                    Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    UserId = dto.UserId,
                    Date = DateTime.Parse(dto.Date),
                    Time = TimeSpan.Parse(dto.Time),
                    Timestamp = DateTime.Parse($"{dto.Date}T{dto.Time}"),
                    BloodSugar = dto.BloodSugar,
                    InsulinDosage = dto.InsulinDosage,
                    BodyWeight = dto.BodyWeight,
                    SystolicBP = dto.SystolicBP,
                    DiastolicBP = dto.DiastolicBP,
                    HeartRate = dto.HeartRate,
                    MealContext = dto.MealContext,
                    Notes = dto.Notes
                };

                string status = _service.EvaluateStatus(reading);
                reading.Status = status;

                _context.HealthReadings.Add(reading);
                _context.SaveChanges();
                return Ok(new
                {
                    message = "Reading saved successfully.",
                    status,
                    readingId = reading.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to save reading.",
                    details = ex.Message
                });

            }
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetUserReadings(int userId)
        {
            try
            {
                var readings = _context.HealthReadings
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.Timestamp)
                    .ToList();

                var dtoList = readings.Select(r => new HealthReadingOutputDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Date = r.Date.ToString("yyyy-MM-dd"),
                    Time = r.Time.ToString(@"hh\:mm"),
                    Timestamp = r.Timestamp,
                    BloodSugar = r.BloodSugar,
                    InsulinDosage = r.InsulinDosage,
                    BodyWeight = r.BodyWeight,
                    SystolicBP = r.SystolicBP,
                    DiastolicBP = r.DiastolicBP,
                    HeartRate = r.HeartRate,
                    MealContext = r.MealContext,
                    Notes = r.Notes,
                    Status = r.Status
                }).ToList();

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to fetch readings.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("user/{userId}/summary")]
        public IActionResult GetUserReadingsSummary(int userId)
        {
            try
            {
                var readings = _context.HealthReadings
                    .Where(r => r.UserId == userId)
                    .ToList();

                if (!readings.Any())
                {
                    return Ok(new { message = "No readings found for user.", summary = (object?)null });
                }

                var summary = new
                {
                    BloodSugar = new
                    {
                        Average = readings.Average(r => r.BloodSugar),
                        Min = readings.Min(r => r.BloodSugar),
                        Max = readings.Max(r => r.BloodSugar)
                    },
                    InsulinDosage = new
                    {
                        Average = readings.Average(r => r.InsulinDosage),
                        Min = readings.Min(r => r.InsulinDosage),
                        Max = readings.Max(r => r.InsulinDosage)
                    },
                    BodyWeight = new
                    {
                        Average = readings.Where(r => r.BodyWeight.HasValue).Any() ? readings.Where(r => r.BodyWeight.HasValue).Average(r => r.BodyWeight.Value) : (double?)null,
                        Min = readings.Where(r => r.BodyWeight.HasValue).Any() ? readings.Where(r => r.BodyWeight.HasValue).Min(r => r.BodyWeight.Value) : (double?)null,
                        Max = readings.Where(r => r.BodyWeight.HasValue).Any() ? readings.Where(r => r.BodyWeight.HasValue).Max(r => r.BodyWeight.Value) : (double?)null
                    },
                    SystolicBP = new
                    {
                        Average = readings.Where(r => r.SystolicBP.HasValue).Any() ? readings.Where(r => r.SystolicBP.HasValue).Average(r => r.SystolicBP.Value) : (double?)null,
                        Min = readings.Where(r => r.SystolicBP.HasValue).Any() ? readings.Where(r => r.SystolicBP.HasValue).Min(r => r.SystolicBP.Value) : (int?)null,
                        Max = readings.Where(r => r.SystolicBP.HasValue).Any() ? readings.Where(r => r.SystolicBP.HasValue).Max(r => r.SystolicBP.Value) : (int?)null
                    },
                    DiastolicBP = new
                    {
                        Average = readings.Where(r => r.DiastolicBP.HasValue).Any() ? readings.Where(r => r.DiastolicBP.HasValue).Average(r => r.DiastolicBP.Value) : (double?)null,
                        Min = readings.Where(r => r.DiastolicBP.HasValue).Any() ? readings.Where(r => r.DiastolicBP.HasValue).Min(r => r.DiastolicBP.Value) : (int?)null,
                        Max = readings.Where(r => r.DiastolicBP.HasValue).Any() ? readings.Where(r => r.DiastolicBP.HasValue).Max(r => r.DiastolicBP.Value) : (int?)null
                    },
                    HeartRate = new
                    {
                        Average = readings.Where(r => r.HeartRate.HasValue).Any() ? readings.Where(r => r.HeartRate.HasValue).Average(r => r.HeartRate.Value) : (double?)null,
                        Min = readings.Where(r => r.HeartRate.HasValue).Any() ? readings.Where(r => r.HeartRate.HasValue).Min(r => r.HeartRate.Value) : (int?)null,
                        Max = readings.Where(r => r.HeartRate.HasValue).Any() ? readings.Where(r => r.HeartRate.HasValue).Max(r => r.HeartRate.Value) : (int?)null
                    }
                };

                return Ok(new { summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to fetch summary.",
                    details = ex.Message
                });
            }
        }
    }
}
