using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using cloud_development_assignment_backend.Data;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly ILogger<PatientController> _logger;
        private readonly AppDbContext _context;

        public PatientController(AppDbContext context, ILogger<PatientController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Patient
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllPatients()
        {
            _logger.LogInformation("Getting all patients");
            var patients = _context.PatientMedicalInfo
                .Include(p => p.User)
                .Select(p => new {
                    p.PatientId,
                    PatientName = p.User != null ? p.User.FirstName + " " + p.User.LastName : "",
                    p.DiabetesType,
                    p.DiagnosisDate,
                    p.LastAppointment
                })
                .ToList();
            return Ok(patients);
        }

        // GET: api/Patient/{id}
        [HttpGet("{id}")]
        public ActionResult<PatientOutputDto> GetPatientById(int id)
        {
            var patient = _context.PatientMedicalInfo
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientId == id);
            if (patient == null)
                return NotFound();

            var output = new PatientOutputDto
            {
                PatientId = patient.PatientId,
                PatientName = patient.User != null ? patient.User.FirstName + " " + patient.User.LastName : "",
                DiabetesType = patient.DiabetesType,
                DiagnosisDate = patient.DiagnosisDate,
                LastAppointment = patient.LastAppointment
            };
            return Ok(output);
        }

        // POST: api/Patient
        [HttpPost]
        public ActionResult<PatientOutputDto> CreatePatient(PatientDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Patient data is null");
            }

            // Only require PatientId
            if (dto.PatientId == 0)
            {
                return BadRequest("PatientId is required");
            }

            // Link to existing user
            var user = _context.Users.FirstOrDefault(u => u.Id == dto.PatientId);
            if (user == null)
            {
                return BadRequest("User with the given PatientId does not exist");
            }

            var patient = new Patient
            {
                PatientId = dto.PatientId,
                DiabetesType = dto.DiabetesType,
                DiagnosisDate = dto.DiagnosisDate,
                LastAppointment = dto.LastAppointment
            };

            _context.PatientMedicalInfo.Add(patient);
            _context.SaveChanges();
            _logger.LogInformation($"Created new patient with ID: {patient.PatientId}");

            // Return PatientOutputDto with PatientName from User
            var output = new PatientOutputDto
            {
                PatientId = patient.PatientId,
                PatientName = user.FirstName + " " + user.LastName,
                DiabetesType = patient.DiabetesType,
                DiagnosisDate = patient.DiagnosisDate,
                LastAppointment = patient.LastAppointment
            };

            return CreatedAtAction(nameof(GetPatientById), new { id = patient.PatientId }, output);
        }

        // PUT: api/Patient/5
        [HttpPut("{id}")]
        public IActionResult UpdatePatientInfo(int id, [FromBody] PatientDto dto)
        {
            if (dto == null || dto.PatientId != id)
                return BadRequest("Invalid data");

            var patient = _context.PatientMedicalInfo.FirstOrDefault(p => p.PatientId == id);
            if (patient == null)
                return NotFound();

            // Only update allowed fields
            patient.DiagnosisDate = dto.DiagnosisDate;
            patient.DiabetesType = dto.DiabetesType;

            _context.SaveChanges();
            return NoContent();
        }

        // DELETE: api/Patient/5
        [HttpDelete("{id}")]
        public IActionResult DeletePatient(int id)
        {
            var patient = _context.PatientMedicalInfo.FirstOrDefault(p => p.PatientId == id);

            if (patient == null)
            {
                _logger.LogWarning($"Attempted to delete non-existent patient with ID: {id}");
                return NotFound();
            }

            _context.PatientMedicalInfo.Remove(patient);
            _context.SaveChanges();
            _logger.LogInformation($"Deleted patient with ID: {id}");

            return NoContent();
        }

        // GET: api/Patient/search?searchTerm=Smith
        [HttpGet("search")]
        public ActionResult<IEnumerable<object>> SearchPatients([FromQuery] string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var matchingPatients = _context.PatientMedicalInfo
                .Include(p => p.User)
                .Where(p => (p.User.FirstName + " " + p.User.LastName).Contains(searchTerm))
                .Select(p => new {
                    p.PatientId,
                    PatientName = p.User.FirstName + " " + p.User.LastName,
                    p.DiabetesType,
                    p.DiagnosisDate,
                    p.LastAppointment
                })
                .ToList();

            _logger.LogInformation($"Searched for patients with term: {searchTerm}, found {matchingPatients.Count} matches");
            return Ok(matchingPatients);
        }

        // GET: api/Patient/filter?diabetesType=Type 1
        [HttpGet("filter")]
        public ActionResult<IEnumerable<Patient>> FilterPatientsByDiabetesType([FromQuery] string diabetesType)
        {
            if (string.IsNullOrEmpty(diabetesType))
            {
                return BadRequest("Diabetes type is required");
            }

            var filteredPatients = _context.PatientMedicalInfo
                .Where(p => p.DiabetesType == diabetesType)
                .ToList();

            _logger.LogInformation($"Filtered patients by diabetes type: {diabetesType}, found {filteredPatients.Count} matches");
            return Ok(filteredPatients);
        }
    }
}