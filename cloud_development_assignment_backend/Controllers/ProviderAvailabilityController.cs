using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Models;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProviderAvailabilityController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProviderAvailabilityController(AppDbContext context) {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var availability = await _context.ProviderAvailabilities
                    .Include(p => p.User)
                    .Where(p => p.ProviderId == id) // ← This filters by ProviderId (doctor/dietician)
                    .ToListAsync();
                if (availability == null)
                    return NotFound($"Availability with ID {id} not found.");
                return Ok(availability);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the availability: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProviderAvailabilityDto dto)
        {
            try
            {
                var availability = new ProviderAvailability
                {
                    ProviderId = dto.ProviderId,
                    AvailabilityDate = dto.AvailabilityDate.Date,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Notes = dto.Notes
                };

                _context.ProviderAvailabilities.Add(availability);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(Get), new { id = availability.Id }, availability);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the availability: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id,  [FromBody] ProviderAvailabilityDto dto)
        {
            try
            {
                var availability = await _context.ProviderAvailabilities.FindAsync(id);
                if (availability == null)
                    return NotFound($"Availability with ID {id} not found.");

                availability.AvailabilityDate = dto.AvailabilityDate.Date;
                availability.StartTime = dto.StartTime;
                availability.EndTime = dto.EndTime;
                availability.Notes = dto.Notes;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the availability: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var availability = await _context.ProviderAvailabilities.FindAsync(id);

                if (availability == null)
                    return NotFound($"Availability with ID {id} not found.");

                _context.ProviderAvailabilities.Remove(availability);
                await _context.SaveChangesAsync();

                return NoContent();

            }
            catch(Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting availability: {ex.Message}");
            }
        }
    }
}
