using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Services;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicalSupplyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly MedicalSupplyStatusService _statusService;

        public MedicalSupplyController(AppDbContext context, MedicalSupplyStatusService statusService)
        {
            _context = context;
            _statusService = statusService;
        }

        //Post 
        [HttpPost]

        public async Task<IActionResult> Create(MedicalSupplyDto dto)
        {
            try
            {
                var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.FamilyId);

                if (user == null)
                {
                    return BadRequest(new { message = "No user found for the specified FamilyId (UserId)." });
                }

                var supply = new MedicalSupply
                {
                    FamilyId = dto.FamilyId,
                    PatientId = user.PatientId.Value,
                    MedicineName = dto.MedicineName,
                    MedicineDescription = dto.MedicineDescription,
                    Quantity = dto.Quantity,
                    Unit = dto.Unit,
                    PlaceToPurchase = dto.PlaceToPurchase,
                    ExpirationDate = dto.ExpirationDate,
                    Notes = dto.Notes,
                    Status = _statusService.GetStatus(dto.Quantity),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.MedicalSupply.Add(supply);
                await _context.SaveChangesAsync();

                return Ok(supply);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("family/{familyId}")]
        public async Task<IActionResult> GetByFamilyId(int familyId)
        {
            try
            {
                var supplies = await _context.MedicalSupply
                    .Where(s => s.FamilyId == familyId)
                    .ToListAsync();

                return Ok(supplies);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, MedicalSupplyDto dto)
        {
            try
            {
                var supply = await _context.MedicalSupply.FindAsync(id);
                if (supply == null)
                {
                    return NotFound();
                }

                supply.MedicineName = dto.MedicineName;
                supply.MedicineDescription = dto.MedicineDescription;
                supply.Quantity = dto.Quantity;
                supply.Unit = dto.Unit;
                supply.PlaceToPurchase = dto.PlaceToPurchase;
                supply.ExpirationDate = dto.ExpirationDate;
                supply.Notes = dto.Notes;
                supply.Status = _statusService.GetStatus(dto.Quantity);
                supply.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(supply);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/MedicalSupply/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var supply = await _context.MedicalSupply.FindAsync(id);
                if (supply == null)
                {
                    return NotFound();
                }

                _context.MedicalSupply.Remove(supply);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


    }
}
