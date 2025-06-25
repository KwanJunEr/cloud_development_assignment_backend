using System.ComponentModel.DataAnnotations;

namespace cloud_development_assignment_backend.DTO
{
    public class LoginRequest
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
    }
}
