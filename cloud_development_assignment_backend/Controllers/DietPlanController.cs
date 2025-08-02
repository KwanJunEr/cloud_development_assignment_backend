using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Data;
using System;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DietPlanController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DietPlanController(AppDbContext context)
        {
            _context = context;
        }

        //POST
        [HttpPost]
        public async Task<IActionResult> CreateDietPlan([FromBody] DietPlanCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var dietPlan = new DietPlan
                {
                    DieticianId = dto.DieticianId,
                    PatientId = dto.PatientId,
                    MealType = dto.MealType,
                    MealPlan = dto.MealPlan,
                    CreatedDate = dto.CreatedDate.Date,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DietPlan.Add(dietPlan);
                await _context.SaveChangesAsync();

                return Ok(dietPlan);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //Update by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDietPlan(int id, [FromBody] DietPlanUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var dietPlan = await _context.DietPlan.FindAsync(id);
                if (dietPlan == null)
                    return NotFound("Diet plan not found.");
                dietPlan.MealType = dto.MealType;
                dietPlan.MealPlan = dto.MealPlan;
                dietPlan.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(dietPlan);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDietPlan(int id)
        {
            try
            {
                var dietPlan = await _context.DietPlan.FindAsync(id);
                if (dietPlan == null)
                    return NotFound("Diet plan not found.");

                _context.DietPlan.Remove(dietPlan);
                await _context.SaveChangesAsync();

                return Ok("Diet plan deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        //Get Controller for Patients
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetDietPlansByPatientId(int patientId)
        {
            try
            {
                var dietPlans = await (
            from dp in _context.DietPlan
            join u in _context.Users on dp.DieticianId equals u.Id
            where dp.PatientId == patientId
            select new DietPlanResponseDto
            {
                Id = dp.Id,
                DieticianId = dp.DieticianId,
                PatientId = dp.PatientId,
                MealType = dp.MealType,
                MealPlan = dp.MealPlan,
                CreatedDate = dp.CreatedDate,
                CreatedAt = dp.CreatedAt,
                UpdatedAt = dp.UpdatedAt,
                DieticianName = u.FirstName + " " + u.LastName,
                DieticianSpecialization = u.Specialization,
                DieticianHospital = u.Hospital
            })
            .ToListAsync();

                return Ok(dietPlans);

            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("dietician/{dieticianId}")]
        public async Task<IActionResult> GetDietPlansByDieticianId(int dieticianId)
        {
            try
            {
                var dietPlans = await (
                    from dp in _context.DietPlan
                    join u in _context.Users on dp.DieticianId equals u.Id
                    where dp.DieticianId == dieticianId
                    select new DietPlanResponseDto
                    {
                        Id = dp.Id,
                        DieticianId = dp.DieticianId,
                        PatientId = dp.PatientId,
                        MealType = dp.MealType,
                        MealPlan = dp.MealPlan,
                        CreatedDate = dp.CreatedDate,
                        CreatedAt = dp.CreatedAt,
                        UpdatedAt = dp.UpdatedAt,
                        DieticianName = u.FirstName + " " + u.LastName,
                        DieticianSpecialization = u.Specialization,
                        DieticianHospital = u.Hospital
                    })
                .ToListAsync();

                return Ok(dietPlans);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDietPlanById(int id)
        {
            try
            {
                var dietPlan = await (
                    from dp in _context.DietPlan
                    join u in _context.Users on dp.DieticianId equals u.Id
                    where dp.Id == id
                    select new DietPlanResponseDto
                    {
                        Id = dp.Id,
                        DieticianId = dp.DieticianId,
                        PatientId = dp.PatientId,
                        MealType = dp.MealType,
                        MealPlan = dp.MealPlan,
                        CreatedDate = dp.CreatedDate,
                        CreatedAt = dp.CreatedAt,
                        UpdatedAt = dp.UpdatedAt,
                        DieticianName = u.FirstName + " " + u.LastName,
                        DieticianSpecialization = u.Specialization,
                        DieticianHospital = u.Hospital
                    })
                .FirstOrDefaultAsync();

                if (dietPlan == null)
                    return NotFound("Diet plan not found.");

                return Ok(dietPlan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("dietician-patient/{dieticianId}")]
        public async Task<IActionResult> GetAppointmentsByDieticianId(int dieticianId)
        {
            try
            {
                        var appointments = await (
                   from appt in _context.PatientAppointmentBooking
                   join patient in _context.Users on appt.PatientID equals patient.Id
                   where appt.ProviderID == dieticianId
                         && appt.Status != "Cancelled"
                   select new PatientAppointmentResponseDto
                   {
                        Id = appt.Id,
                        PatientID = appt.PatientID,
                        ProviderID = appt.ProviderID,
                        Role = appt.Role,
                        ProviderName = appt.ProviderName,
                        ProviderSpecialization = appt.ProviderSpecialization,
                        ProviderVenue = appt.ProviderVenue,
                        ProviderAvailableDate = appt.ProviderAvailableDate,
                        ProviderAvailableTimeSlot = appt.ProviderAvailableTimeSlot,
                        BookingMode = appt.BookingMode,
                        ServiceBooked = appt.ServiceBooked,
                        ReasonsForVisit = appt.ReasonsForVisit,
                        Status = appt.Status,
                        PatientFullName = patient.FirstName + " " + patient.LastName
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
