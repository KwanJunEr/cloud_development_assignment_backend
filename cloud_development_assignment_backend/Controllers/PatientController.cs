using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly ILogger<PatientController> _logger;
        // KCG: use a repository or service here
        private static readonly List<Patient> _patients = new List<Patient>
        {
            // sample data
            new Patient
            {
                Id = "p1",
                Name = "John Doe",
                Age = 45,
                Gender = "Male",
                Email = "john.doe@example.com",
                Phone = "555-123-4567",
                Address = "123 Main St, Anytown, USA",
                DiabetesType = "Type 2",
                DiagnosisDate = DateTime.Parse("2020-03-15"),
                LatestA1c = 7.1,
                EmergencyContact = "Jane Doe (Wife) - 555-123-4568",
                Notes = "Patient is adhering well to treatment plan. Exercise routine needs improvement."
            },
            new Patient
            {
                Id = "p2",
                Name = "Mary Smith",
                Age = 38,
                Gender = "Female",
                Email = "mary.smith@example.com",
                Phone = "555-987-6543",
                Address = "456 Oak Ave, Somewhere, USA",
                DiabetesType = "Type 1",
                DiagnosisDate = DateTime.Parse("2010-06-22"),
                LatestA1c = 6.8,
                EmergencyContact = "Robert Smith (Husband) - 555-987-6544",
                Notes = "Patient manages glucose levels well. Regular insulin adjustment is required."
            }
        };

        public PatientController(ILogger<PatientController> logger)
        {
            _logger = logger;
        }

        // GET: api/Patient
        [HttpGet]
        public ActionResult<IEnumerable<Patient>> GetAllPatients()
        {
            _logger.LogInformation("Getting all patients");
            return Ok(_patients);
        }

        // GET: api/Patient/5
        [HttpGet("{id}")]
        public ActionResult<Patient> GetPatientById(string id)
        {
            _logger.LogInformation($"Getting patient with ID: {id}");
            var patient = _patients.FirstOrDefault(p => p.Id == id);

            if (patient == null)
            {
                _logger.LogWarning($"Patient with ID {id} not found");
                return NotFound();
            }

            return Ok(patient);
        }

        // POST: api/Patient
        [HttpPost]
        public ActionResult<Patient> CreatePatient(Patient patient)
        {
            if (patient == null)
            {
                return BadRequest("Patient data is null");
            }

            if (string.IsNullOrEmpty(patient.Name) ||
                string.IsNullOrEmpty(patient.Gender) ||
                string.IsNullOrEmpty(patient.DiabetesType))
            {
                return BadRequest("Name, Gender, and DiabetesType are required fields");
            }

            if (string.IsNullOrEmpty(patient.Id))
            {
                patient.Id = $"p{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            _patients.Add(patient);
            _logger.LogInformation($"Created new patient with ID: {patient.Id}");

            return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, patient);
        }

        // PUT: api/Patient/5
        [HttpPut("{id}")]
        public IActionResult UpdatePatient(string id, Patient patient)
        {
            if (patient == null || id != patient.Id)
            {
                return BadRequest("Invalid patient data or ID mismatch");
            }

            var existingPatient = _patients.FirstOrDefault(p => p.Id == id);

            if (existingPatient == null)
            {
                _logger.LogWarning($"Attempted to update non-existent patient with ID: {id}");
                return NotFound();
            }

            var index = _patients.IndexOf(existingPatient);
            _patients[index] = patient;
            _logger.LogInformation($"Updated patient with ID: {id}");

            return NoContent();
        }

        // DELETE: api/Patient/5
        [HttpDelete("{id}")]
        public IActionResult DeletePatient(string id)
        {
            var patient = _patients.FirstOrDefault(p => p.Id == id);

            if (patient == null)
            {
                _logger.LogWarning($"Attempted to delete non-existent patient with ID: {id}");
                return NotFound();
            }

            _patients.Remove(patient);
            _logger.LogInformation($"Deleted patient with ID: {id}");

            return NoContent();
        }

        // GET: api/Patient/search?searchTerm=Smith
        [HttpGet("search")]
        public ActionResult<IEnumerable<Patient>> SearchPatients([FromQuery] string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var matchingPatients = _patients
                .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation($"Searched for patients with term: {searchTerm}, found {matchingPatients.Count} matches");
            return Ok(matchingPatients);
        }

        // GET: api/Patient/filter?diabetesType=Type1
        [HttpGet("filter")]
        public ActionResult<IEnumerable<Patient>> FilterPatientsByDiabetesType([FromQuery] string diabetesType)
        {
            if (string.IsNullOrEmpty(diabetesType))
            {
                return BadRequest("Diabetes type is required");
            }

            var filteredPatients = _patients
                .Where(p => p.DiabetesType.Equals(diabetesType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation($"Filtered patients by diabetes type: {diabetesType}, found {filteredPatients.Count} matches");
            return Ok(filteredPatients);
        }
    }
}
