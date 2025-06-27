using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using cloud_development_assignment_backend.DTO;
using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Data;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionController : ControllerBase
    {
        private readonly ILogger<PrescriptionController> _logger;
        private readonly AppDbContext _context;

        public PrescriptionController(AppDbContext context, ILogger<PrescriptionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Prescription
        [HttpGet]
        public ActionResult<IEnumerable<PrescriptionOutputDto>> GetAllPrescriptions()
        {
            _logger.LogInformation("Getting all prescriptions");
            var prescriptions = _context.Prescriptions
                .Include(p => p.Medications)
                .ToList();

            var result = prescriptions.Select(p => new PrescriptionOutputDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                Date = p.Date,
                Notes = p.Notes,
                PhysicianId = p.PhysicianId,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Medications = p.Medications.Select(m => new MedicationOutputDto
                {
                    Id = m.Id,
                    PrescriptionId = m.PrescriptionId,
                    Name = m.Name,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Notes = m.Notes,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList()
            }).ToList();
            return Ok(result);
        }

        // GET: api/Prescription/{id}
        [HttpGet("{id}")]
        public ActionResult<PrescriptionOutputDto> GetPrescriptionById(string id)
        {
            _logger.LogInformation($"Getting prescription with ID: {id}");
            var p = _context.Prescriptions
                .Include(x => x.Medications)
                .FirstOrDefault(p => p.Id.ToString() == id);

            if (p == null)
            {
                return NotFound();
            }

            var dto = new PrescriptionOutputDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                Date = p.Date,
                Notes = p.Notes,
                PhysicianId = p.PhysicianId,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Medications = p.Medications.Select(m => new MedicationOutputDto
                {
                    Id = m.Id,
                    PrescriptionId = m.PrescriptionId,
                    Name = m.Name,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Notes = m.Notes,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList()
            };

            return Ok(dto);
        }

        // GET: api/Prescription/patient/{patientId}
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<PrescriptionOutputDto>> GetPrescriptionsByPatientId(string patientId)
        {
            _logger.LogInformation($"Getting prescriptions for patient ID: {patientId}");
            if (string.IsNullOrEmpty(patientId))
            {
                return BadRequest("Patient ID is required");
            }

            var prescriptions = _context.Prescriptions
                .Include(p => p.Medications)
                .Where(p => p.PatientId.ToString() == patientId)
                .OrderByDescending(p => p.Date)
                .ToList();

            var result = prescriptions.Select(p => new PrescriptionOutputDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                Date = p.Date,
                Notes = p.Notes,
                PhysicianId = p.PhysicianId,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Medications = p.Medications.Select(m => new MedicationOutputDto
                {
                    Id = m.Id,
                    PrescriptionId = m.PrescriptionId,
                    Name = m.Name,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Notes = m.Notes,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        // POST: api/Prescription
        [HttpPost]
        public ActionResult<PrescriptionOutputDto> CreatePrescription(PrescriptionDto dto)
        {
            _logger.LogInformation($"Creating prescription: {JsonSerializer.Serialize(dto)}");
            if (dto == null)
            {
                return BadRequest("Prescription data is required");
            }

            if (dto.PatientId == 0)
            {
                return BadRequest("PatientId is a required field");
            }

            var prescription = new Prescription
            {
                PatientId = dto.PatientId,
                Date = dto.Date,
                Notes = dto.Notes,
                PhysicianId = dto.PhysicianId,
                CreatedAt = DateTime.UtcNow,
                Medications = new List<Medication>()
            };

            if (dto.Medications != null)
            {
                foreach (var m in dto.Medications)
                {
                    prescription.Medications.Add(new Medication
                    {
                        Name = m.Name,
                        Dosage = m.Dosage,
                        Frequency = m.Frequency,
                        Duration = m.Duration,
                        Notes = m.Notes,
                        StartDate = m.StartDate,
                        EndDate = m.EndDate,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            _context.Prescriptions.Add(prescription);
            _context.SaveChanges();
            _logger.LogInformation($"Prescription created with ID: {prescription.Id}");

            // Fetch with medications for output
            var created = _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefault(p => p.Id == prescription.Id);

            var output = new PrescriptionOutputDto
            {
                Id = created.Id,
                PatientId = created.PatientId,
                Date = created.Date,
                Notes = created.Notes,
                PhysicianId = created.PhysicianId,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt,
                Medications = created.Medications.Select(m => new MedicationOutputDto
                {
                    Id = m.Id,
                    PrescriptionId = m.PrescriptionId,
                    Name = m.Name,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Notes = m.Notes,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                }).ToList()
            };

            return CreatedAtAction(nameof(GetPrescriptionById), new { id = created.Id }, output);
        }

        // PUT: api/Prescription/{id}
        [HttpPut("{id}")]
        public IActionResult UpdatePrescription(int id, PrescriptionDto dto)
        {
            _logger.LogInformation($"Updating prescription with ID: {id}");
            if (dto == null)
            {
                return BadRequest("Invalid prescription data");
            }

            var existingPrescription = _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefault(p => p.Id == id);

            if (existingPrescription == null)
            {
                return NotFound("Prescription not found");
            }

            existingPrescription.PatientId = dto.PatientId;
            existingPrescription.Date = dto.Date;
            existingPrescription.Notes = dto.Notes;
            existingPrescription.PhysicianId = dto.PhysicianId;
            existingPrescription.UpdatedAt = DateTime.UtcNow;

            _context.Medications.RemoveRange(existingPrescription.Medications);

            existingPrescription.Medications = dto.Medications?.Select(m => new Medication
            {
                Name = m.Name,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }).ToList() ?? new List<Medication>();

            _context.SaveChanges();
            return NoContent();
        }

        // DELETE: api/Prescription/{id}
        [HttpDelete("{id}")]
        public IActionResult DeletePrescription(string id)
        {
            _logger.LogInformation($"Deleting prescription with ID: {id}");
            var prescription = _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefault(p => p.Id.ToString() == id);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            _context.Medications.RemoveRange(prescription.Medications);
            _context.Prescriptions.Remove(prescription);
            _context.SaveChanges();

            return NoContent();
        }

        // POST: api/Prescription/{prescriptionId}/medications
        [HttpPost("{prescriptionId}/medications")]
        public ActionResult<Medication> AddMedicationToPrescription(int prescriptionId, MedicationDto medicationDto)
        {
            _logger.LogInformation($"Adding medication to prescription ID: {prescriptionId}");
            if (medicationDto == null)
            {
                return BadRequest("Medication data is required");
            }

            var prescription = _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefault(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            var medication = new Medication
            {
                Name = medicationDto.Name,
                Dosage = medicationDto.Dosage,
                Frequency = medicationDto.Frequency,
                Duration = medicationDto.Duration,
                Notes = medicationDto.Notes,
                StartDate = medicationDto.StartDate,
                EndDate = medicationDto.EndDate,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                PrescriptionId = prescriptionId
            };

            prescription.Medications.Add(medication);
            _context.SaveChanges();
            _logger.LogInformation($"Medication added with ID: {medication.Id}");

            return CreatedAtAction(nameof(GetPrescriptionById), new { id = prescriptionId }, medication);
        }

        // DELETE: api/Prescription/{prescriptionId}/medications/{medicationId}
        [HttpDelete("{prescriptionId}/medications/{medicationId}")]
        public IActionResult RemoveMedicationFromPrescription(int prescriptionId, int medicationId)
        {
            _logger.LogInformation($"Removing medication ID: {medicationId} from prescription ID: {prescriptionId}");
            var prescription = _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefault(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            var medication = prescription.Medications.FirstOrDefault(m => m.Id == medicationId);

            if (medication == null)
            {
                return NotFound("Medication not found");
            }

            _context.Medications.Remove(medication);
            _context.SaveChanges();

            return NoContent();
        }

        // GET: api/Prescription/{prescriptionId}/medications
        [HttpGet("{prescriptionId}/medications")]
        public ActionResult<IEnumerable<MedicationOutputDto>> GetMedicationsForPrescription(int prescriptionId)
        {
            _logger.LogInformation($"Getting medications for prescription ID: {prescriptionId}");
            var prescription = _context.Prescriptions
                .Include(p => p.Medications)
                .FirstOrDefault(p => p.Id == prescriptionId);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            var result = prescription.Medications.Select(m => new MedicationOutputDto
            {
                Id = m.Id,
                PrescriptionId = m.PrescriptionId,
                Name = m.Name,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes,
                StartDate = m.StartDate,
                EndDate = m.EndDate,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList();

            return Ok(result);
        }
    }
}
