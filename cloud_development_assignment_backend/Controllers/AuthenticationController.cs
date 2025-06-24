using Microsoft.AspNetCore.Mvc;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;
using System.Security.Cryptography;
using System.Text;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRequest request)
        {
            try
            {
                if (_context.Users.Any(u => u.Email == request.Email))
                {
                    return BadRequest("Email already exists.");
                }

                var user = new User
                {
                    Role = request.Role,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    Phone = request.Phone,
                    Specialization = request.Specialization,
                    Hospital = request.Hospital,
                    LicenseNumber = request.LicenseNumber,
                    PatientId = request.PatientId,
                    Relationship = request.Relationship
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User registered successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }

        // Moved this inside the class
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
