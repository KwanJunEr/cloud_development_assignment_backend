using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;


namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicationRemindersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MedicationRemindersController(AppDbContext context)
        {
            _context = context;
        }

        //POST : api/MedicationRemidners 
        [HttpPost]
        public async Task<IActionResult> CreateMedicationReminder([FromBody] MedicationReminderDto dto)
        {
            try
            {

                // Check if the user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
                if (!userExists)
                {
                    return BadRequest($"User with ID {dto.UserId} does not exist.");
                }

                var reminder = new MedicationReminder
                {
                    UserId = dto.UserId,
                    MedicationName = dto.MedicationName,
                    Description = dto.Description,
                    Dosage = dto.Dosage,
                    ReminderDate = dto.ReminderDate,
                    ReminderDue = dto.ReminderDue,
                    Notes = dto.Notes,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MedicationReminders.Add(reminder);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Medication Reminder created sucessfully",
                    reminder

                });

            }
            catch (Exception ex)
            {

                return StatusCode(500, $"An error occurred while creating the medication reminder: {ex.Message}");

            }
        }

        //
        [HttpGet("user/{userId}/upcoming")]
        public async Task<ActionResult<IEnumerable<MedicationReminderDto>>> GetUpcomingRemindersByUser(int userId)
        {
            try
            {
                // 1. Validate user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return NotFound($"User with ID {userId} does not exist.");
                }

                // 2. Get today's date
                var today = DateTime.UtcNow.Date;

                // 3. Query reminders
                var reminders = await _context.MedicationReminders
                    .Where(r => r.UserId == userId && r.ReminderDate >= today)
                    .OrderBy(r => r.ReminderDate)
                    .Select(r => new MedicationReminderDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        MedicationName = r.MedicationName,
                        Description = r.Description,
                        Dosage = r.Dosage,
                        ReminderDate = r.ReminderDate,
                        ReminderDue = r.ReminderDue,
                        Notes = r.Notes,
                        Status = r.Status,
                    })
                    .ToListAsync();

                return Ok(reminders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving reminders: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMedicationReminder (int id, [FromBody] MedicationReminderDto dto)
        {
            try
            {
                var reminder = await _context.MedicationReminders.FindAsync(id);
                if(reminder == null)
                {
                    return NotFound($"Medication Reminder with ID {id} not found.");
                }

                // Validate Status
                var validStatuses = new[] { "pending", "taken" };
                if (!validStatuses.Contains(dto.Status.ToLower()))
                {
                    return BadRequest("Invalid status. Valid values: 'pending', 'taken'.");
                }

                // Update fields
                reminder.MedicationName = dto.MedicationName;
                reminder.Description = dto.Description;
                reminder.Dosage = dto.Dosage;
                reminder.ReminderDate = dto.ReminderDate;
                reminder.ReminderDue = dto.ReminderDue;
                reminder.Notes = dto.Notes;
                reminder.Status = dto.Status.ToLower();
                reminder.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Medication Reminder updated successfully.",
                    reminder
                });


            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the reminder: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedicationReminder(int id)
        {
            try
            {
                var reminder = await _context.MedicationReminders.FindAsync(id);
                if (reminder == null)
                {
                    return NotFound($"Medication Reminder with ID {id} not found.");
                }

                _context.MedicationReminders.Remove(reminder);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Medication Reminder deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the reminder: {ex.Message}");
            }
        }

    }
}
