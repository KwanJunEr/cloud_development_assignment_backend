using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DietTipController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DietTipController(AppDbContext context)
        {
            _context = context;
        }

        //POST : api/DietTip
        [HttpPost]
        public async Task<ActionResult<DietTip>> CreateDietTip(CreateDietTipDto dto)
        {
            try
            {
                var dietTip = new DietTip
                {
                    DieticianId = dto.DieticianId,
                    Title = dto.Title,
                    Content = dto.Content,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DietTip.Add(dietTip);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =  "Created DietTip Successfully",
                    dietTip

                });

            }catch(Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DietTip>>> GetAllDietTips()
        {
            try
            {
                var tips = await _context.DietTip.ToListAsync();
                return Ok(new
                {
                    message = "Return all diettips",
                    tips
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        // Get by dietician
        [HttpGet("dietician/{dieticianId}")]
        public async Task<ActionResult> GetByDieticianId(int dieticianId)
        {
            try
            {
                var tips = await _context.DietTip
                    .Where(t => t.DieticianId == dieticianId)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Retrieve all records for the individual dietician",
                    tips
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        //Update based on the diet record 
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDietTip(int id, CreateDietTipDto dto)
        {
            try
            {
                var dietTip = await _context.DietTip.FindAsync(id);
                if(dietTip == null)
                {
                    return NotFound(new { message = "DietTip not found" });
                }

                dietTip.Title = dto.Title;
                dietTip.Content = dto.Content;
                dietTip.UpdatedAt = DateTime.UtcNow;

                return Ok(new
                {
                    message = "DietTip updated successfully",
                    dietTip
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        //Delete by indivudal record 
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDietTip(int id)
        {
            try
            {
                var dietTip = await _context.DietTip.FindAsync(id);
                if (dietTip == null)
                    return NotFound(new { message = "DietTip not found" });

                _context.DietTip.Remove(dietTip);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "DietTip deleted successfully",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }


    }
}
