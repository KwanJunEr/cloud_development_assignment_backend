using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionController : ControllerBase
    {
        private readonly ILogger<PrescriptionController> _logger;
        // KCG:use a repository or service here
        private static readonly List<Prescription> _prescriptions = new List<Prescription>();

        public PrescriptionController(ILogger<PrescriptionController> logger)
        {
            _logger = logger;

            // Initialize with sample data if empty
            // KCG: jsut for demo , remove if  real data
            if (!_prescriptions.Any())
            {
                // Sample data for John Doe (p1)
                var p1Prescription1 = new Prescription
                {
                    Id = "presc1",
                    PatientId = "p1",
                    Date = DateTime.Parse("2023-11-15"),
                    Notes = "Patient reports good compliance with medication regimen",
                    PhysicianId = "phy1",
                    PhysicianName = "Dr. Smith",
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2023-11-15")
                };

                p1Prescription1.Medications = new List<Medication>
                {
                    new Medication
                    {
                        Id = "med1",
                        PrescriptionId = "presc1",
                        Name = "Metformin",
                        Dosage = "500mg",
                        Frequency = "Twice daily with meals",
                        Duration = "90 days",
                        Notes = "Take with food to reduce GI side effects",
                        StartDate = DateTime.Parse("2023-11-15"),
                        CreatedAt = DateTime.Parse("2023-11-15"),
                        IsActive = true
                    },
                    new Medication
                    {
                        Id = "med2",
                        PrescriptionId = "presc1",
                        Name = "Lisinopril",
                        Dosage = "10mg",
                        Frequency = "Once daily",
                        Duration = "90 days",
                        Notes = "For blood pressure control",
                        StartDate = DateTime.Parse("2023-11-15"),
                        CreatedAt = DateTime.Parse("2023-11-15"),
                        IsActive = true
                    }
                };

                _prescriptions.Add(p1Prescription1);

                // Another prescription for John Doe (p1) - older one
                // KCG: remove if real data
                var p1Prescription2 = new Prescription
                {
                    Id = "presc2",
                    PatientId = "p1",
                    Date = DateTime.Parse("2023-08-15"),
                    Notes = "Initial prescription after diagnosis",
                    PhysicianId = "phy1",
                    PhysicianName = "Dr. Smith",
                    IsActive = false,
                    CreatedAt = DateTime.Parse("2023-08-15")
                };

                p1Prescription2.Medications = new List<Medication>
                {
                    new Medication
                    {
                        Id = "med3",
                        PrescriptionId = "presc2",
                        Name = "Metformin",
                        Dosage = "250mg",
                        Frequency = "Twice daily with meals",
                        Duration = "30 days",
                        Notes = "Starter dose, will increase if tolerated well",
                        StartDate = DateTime.Parse("2023-08-15"),
                        EndDate = DateTime.Parse("2023-09-15"),
                        CreatedAt = DateTime.Parse("2023-08-15"),
                        IsActive = false
                    }
                };

                _prescriptions.Add(p1Prescription2);

                // Sample data for Mary Smith (p2)
                var p2Prescription = new Prescription
                {
                    Id = "presc3",
                    PatientId = "p2",
                    Date = DateTime.Parse("2023-11-10"),
                    Notes = "Adjusted insulin dosage based on latest glucose readings",
                    PhysicianId = "phy2",
                    PhysicianName = "Dr. Johnson",
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2023-11-10")
                };

                p2Prescription.Medications = new List<Medication>
                {
                    new Medication
                    {
                        Id = "med4",
                        PrescriptionId = "presc3",
                        Name = "Lantus",
                        Dosage = "20 units",
                        Frequency = "Once daily at bedtime",
                        Duration = "30 days",
                        Notes = "Long-acting insulin",
                        StartDate = DateTime.Parse("2023-11-10"),
                        CreatedAt = DateTime.Parse("2023-11-10"),
                        IsActive = true
                    },
                    new Medication
                    {
                        Id = "med5",
                        PrescriptionId = "presc3",
                        Name = "Novolog",
                        Dosage = "Based on carb count",
                        Frequency = "Before meals",
                        Duration = "30 days",
                        Notes = "Rapid-acting insulin, 1 unit per 10g of carbs",
                        StartDate = DateTime.Parse("2023-11-10"),
                        CreatedAt = DateTime.Parse("2023-11-10"),
                        IsActive = true
                    }
                };

                _prescriptions.Add(p2Prescription);
            }
        }

        // GET: api/Prescription
        [HttpGet]
        public ActionResult<IEnumerable<Prescription>> GetAllPrescriptions()
        {
            _logger.LogInformation("Getting all prescriptions");
            return Ok(_prescriptions);
        }

        // GET: api/Prescription/{id}
        [HttpGet("{id}")]
        public ActionResult<Prescription> GetPrescriptionById(string id)
        {
            _logger.LogInformation($"Getting prescription with ID: {id}");
            var prescription = _prescriptions.FirstOrDefault(p => p.Id == id);

            if (prescription == null)
            {
                return NotFound();
            }

            return Ok(prescription);
        }

        // GET: api/Prescription/patient/{patientId}
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<Prescription>> GetPrescriptionsByPatientId(string patientId)
        {
            _logger.LogInformation($"Getting prescriptions for patient ID: {patientId}");
            if (string.IsNullOrEmpty(patientId))
            {
                return BadRequest("Patient ID is required");
            }

            var prescriptions = _prescriptions
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.Date)
                .ToList();

            return Ok(prescriptions);
        }

        // POST: api/Prescription
        [HttpPost]
        public ActionResult<Prescription> CreatePrescription(Prescription prescription)
        {
            _logger.LogInformation($"Creating prescription: {JsonSerializer.Serialize(prescription)}");
            if (prescription == null)
            {
                return BadRequest("Prescription data is required");
            }

            if (string.IsNullOrEmpty(prescription.PatientId))
            {
                return BadRequest("PatientId is a required field");
            }

            if (string.IsNullOrEmpty(prescription.Id))
            {
                prescription.Id = Guid.NewGuid().ToString();
            }

            if (prescription.CreatedAt == default)
            {
                prescription.CreatedAt = DateTime.UtcNow;
            }

            if (prescription.Medications != null)
            {
                foreach (var medication in prescription.Medications)
                {
                    if (string.IsNullOrEmpty(medication.Id))
                    {
                        medication.Id = Guid.NewGuid().ToString();
                    }

                    medication.PrescriptionId = prescription.Id;

                    if (medication.CreatedAt == default)
                    {
                        medication.CreatedAt = DateTime.UtcNow;
                    }

                    if (medication.StartDate == default)
                    {
                        medication.StartDate = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                prescription.Medications = new List<Medication>();
            }

            _prescriptions.Add(prescription);
            _logger.LogInformation($"Prescription created with ID: {prescription.Id}");

            return CreatedAtAction(nameof(GetPrescriptionById), new { id = prescription.Id }, prescription);
        }

        // PUT: api/Prescription/{id}
        [HttpPut("{id}")]
        public IActionResult UpdatePrescription(string id, Prescription prescription)
        {
            _logger.LogInformation($"Updating prescription with ID: {id}");
            if (prescription == null || id != prescription.Id)
            {
                return BadRequest("Invalid prescription data or ID mismatch");
            }

            var existingPrescription = _prescriptions.FirstOrDefault(p => p.Id == id);

            if (existingPrescription == null)
            {
                return NotFound("Prescription not found");
            }

            prescription.UpdatedAt = DateTime.UtcNow;

            prescription.CreatedAt = existingPrescription.CreatedAt;

            var index = _prescriptions.IndexOf(existingPrescription);
            _prescriptions[index] = prescription;

            return NoContent();
        }

        // DELETE: api/Prescription/{id}
        [HttpDelete("{id}")]
        public IActionResult DeletePrescription(string id)
        {
            _logger.LogInformation($"Deleting prescription with ID: {id}");
            var prescription = _prescriptions.FirstOrDefault(p => p.Id == id);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            _prescriptions.Remove(prescription);

            return NoContent();
        }

        // POST: api/Prescription/{prescriptionId}/medications
        [HttpPost("{prescriptionId}/medications")]
        public ActionResult<Medication> AddMedicationToPrescription(string prescriptionId, Medication medication)
        {
            _logger.LogInformation($"Adding medication to prescription ID: {prescriptionId}");
            if (medication == null)
            {
                return BadRequest("Medication data is required");
            }

            var prescription = _prescriptions.FirstOrDefault(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            if (string.IsNullOrEmpty(medication.Id))
            {
                medication.Id = Guid.NewGuid().ToString();
            }

            medication.PrescriptionId = prescriptionId;


            if (medication.CreatedAt == default)
            {
                medication.CreatedAt = DateTime.UtcNow;
            }

            if (medication.StartDate == default)
            {
                medication.StartDate = DateTime.UtcNow;
            }

            prescription.Medications.Add(medication);
            _logger.LogInformation($"Medication added with ID: {medication.Id}");

            return CreatedAtAction(nameof(GetPrescriptionById), new { id = prescriptionId }, medication);
        }

        // DELETE: api/Prescription/{prescriptionId}/medications/{medicationId}
        [HttpDelete("{prescriptionId}/medications/{medicationId}")]
        public IActionResult RemoveMedicationFromPrescription(string prescriptionId, string medicationId)
        {
            _logger.LogInformation($"Removing medication ID: {medicationId} from prescription ID: {prescriptionId}");
            var prescription = _prescriptions.FirstOrDefault(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            var medication = prescription.Medications.FirstOrDefault(m => m.Id == medicationId);

            if (medication == null)
            {
                return NotFound("Medication not found");
            }

            prescription.Medications.Remove(medication);

            return NoContent();
        }

        // GET: api/Prescription/{prescriptionId}/medications
        [HttpGet("{prescriptionId}/medications")]
        public ActionResult<IEnumerable<Medication>> GetMedicationsForPrescription(string prescriptionId)
        {
            _logger.LogInformation($"Getting medications for prescription ID: {prescriptionId}");
            var prescription = _prescriptions.FirstOrDefault(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            return Ok(prescription.Medications);
        }
    }
}
