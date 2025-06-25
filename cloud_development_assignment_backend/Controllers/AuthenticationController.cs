using Microsoft.AspNetCore.Mvc;
using cloud_development_assignment_backend.Models;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Azure.Core;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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

        [HttpPost("login")]
        public IActionResult Login(LoginRequest loginRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
                    return BadRequest("Email and password are required.");

                var user = _context.Users.SingleOrDefault(u => u.Email == loginRequest.Email);
                if (user == null)
                    return Unauthorized("Invalid email or password.");

                var hashed = HashPassword(loginRequest.Password);
                if (user.PasswordHash != hashed)
                    return Unauthorized("Invalid email or password.");

                var token = GenerateJwTToken(user);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Login failed.", error = ex.Message });
            }
        }


        //Generate JWT Token 
        private string GenerateJwTToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
               issuer: _config["Jwt:Issuer"],
               audience: _config["Jwt:Audience"],
               claims: claims,
               expires: DateTime.Now.AddHours(2),
               signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
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
