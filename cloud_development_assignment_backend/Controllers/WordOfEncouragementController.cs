using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.DTO;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WordsofEncouragementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WordsofEncouragementController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] WordofEncouragementDto dto)
        {
            try
            {
                var patient = await _context.Users.FindAsync(dto.PatientId);
                var family = await _context.Users.FindAsync(dto.FamilyId);

                if (patient == null || family == null)
                    return NotFound("Patient or family member not found.");

                var now = DateTime.Now;

                var message = new WordsofEncouragement
                {
                    PatientId = dto.PatientId,
                    FamilyId = dto.FamilyId,
                    Content = dto.Content,
                    MessageDate = now.Date,
                    MessageTime = now.TimeOfDay,
                    CreatedAt = now
                };

                _context.WordsofEncouragement.Add(message);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Word of encouragement posted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while posting the message.", details = ex.Message });
            }
        }

        [HttpGet("patient-by-family/{familyId}")]
        public async Task<IActionResult> GetPatientByFamilyId(int familyId)
        {
            try
            {
                var familyUser = await _context.Users.FindAsync(familyId);
                if (familyUser == null)
                    return NotFound("Family user not found.");

                var result = new PatientFamilyDto
                {
                    FamilyId = familyUser.Id,
                    PatientId = familyUser.PatientId,
                    Relationship = familyUser.Relationship
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error retrieving patient info.", details = ex.Message });
            }
        }

        [HttpGet("by-patient/{patientId}")]
        public async Task<IActionResult> GetMessagesByPatientId(int patientId)
        {
            try
            {
                var messages = await _context.WordsofEncouragement
                    .Where(m => m.PatientId == patientId)
                    .Join(
                        _context.Users,
                        encouragement => encouragement.FamilyId,
                        user => user.Id,
                        (encouragement, user) => new
                        {
                            encouragement.Id,
                            encouragement.PatientId,
                            encouragement.FamilyId,
                            encouragement.Content,
                            encouragement.MessageDate,
                            encouragement.MessageTime,
                            encouragement.CreatedAt,
                            FamilyName = user.FirstName + " " + user.LastName,
                            Relationship = user.Relationship
                        }
                    )
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error fetching messages.",
                    details = ex.Message
                });
            }
        }
    }
}
