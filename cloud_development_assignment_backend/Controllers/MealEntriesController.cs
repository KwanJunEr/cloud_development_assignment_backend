using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;
using System.Threading.Tasks;


namespace cloud_development_assignment_backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MealEntriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MealEntriesController(AppDbContext context)
        {
            _context = context;
        }

        //Create
        [HttpPost]
        public async Task<IActionResult> CreateMealEntry([FromBody] MealEntryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
                if (!userExists)
                    return NotFound($"User with Id {dto.UserId} does not exist.");

                var mealEntry = new MealEntry
                {
                    UserId = dto.UserId,
                    EntryDate = dto.EntryDate,
                    MealType = dto.MealType,
                    FoodItem = dto.FoodItem,
                    Portion = dto.Portion,
                    Notes = dto.Notes
                };

                _context.MealEntries.Add(mealEntry);
                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        message = "Reading saved successfully.",
                        mealEntry
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        //Get all meals by UserId
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetMealEntriesByUserId(int userId)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if(!userExists)
                    return NotFound($"User with Id {userId} does not exist.");

                var meals = await _context.MealEntries
                     .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.EntryDate)
                    .ToListAsync();

                return Ok(meals);
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }

        //Update by Id 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealEntry(int id, [FromBody] MealEntryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var mealEntry = await _context.MealEntries.FindAsync(id);

                if (mealEntry == null)
                    return NotFound();

                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
                if (!userExists)
                    return NotFound($"User with Id {dto.UserId} does not exist.");

                mealEntry.UserId = dto.UserId;
                mealEntry.EntryDate = dto.EntryDate;
                mealEntry.MealType = dto.MealType;
                mealEntry.FoodItem = dto.FoodItem;
                mealEntry.Portion = dto.Portion;
                mealEntry.Notes = dto.Notes;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        // Delete by Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealEntry(int id)
        {
            try
            {
                var mealEntry = await _context.MealEntries.FindAsync(id);

                if (mealEntry == null)
                    return NotFound();

                _context.MealEntries.Remove(mealEntry);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetMealEntriesByPatientId(int patientId)
        {
            try
            {
                var fiveDaysAgo = DateTime.Today.AddDays(-5);

                var mealEntries = await _context.MealEntries
                    .Where(m => m.UserId == patientId && m.EntryDate >= fiveDaysAgo)
                    .OrderByDescending(m => m.EntryDate)
                    .ToListAsync();

                return Ok(mealEntries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
