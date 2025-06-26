using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using cloud_development_assignment_backend.DTO;
using cloud_development_assignment_backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TreatmentPlanController : ControllerBase
    {
        private readonly ILogger<TreatmentPlanController> _logger;
        private readonly AppDbContext _context;

        public TreatmentPlanController(AppDbContext context, ILogger<TreatmentPlanController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TreatmentPlan
        [HttpGet]
        public ActionResult<IEnumerable<TreatmentPlanOutputDto>> GetAllTreatmentPlans()
        {
            var result = _context.TreatmentPlans
                .Select(tp => new TreatmentPlanOutputDto
                {
                    Id = tp.Id,
                    PatientId = tp.PatientId,
                    Date = tp.Date,
                    Diagnosis = tp.Diagnosis,
                    TreatmentGoals = tp.TreatmentGoals,
                    DietaryRecommendations = tp.DietaryRecommendations,
                    ExerciseRecommendations = tp.ExerciseRecommendations,
                    MedicationNotes = tp.MedicationNotes,
                    FollowUpDate = tp.FollowUpDate,
                    CreatedBy = tp.CreatedBy,
                    CreatedAt = tp.CreatedAt,
                    UpdatedAt = tp.UpdatedAt
                }).ToList();
            return Ok(result);
        }

        // GET: api/TreatmentPlan/5
        [HttpGet("{id}")]
        public ActionResult<TreatmentPlanOutputDto> GetTreatmentPlanById(int id)
        {
            var tp = _context.TreatmentPlans.FirstOrDefault(p => p.Id == id);
            if (tp == null)
                return NotFound();
            var dto = new TreatmentPlanOutputDto
            {
                Id = tp.Id,
                PatientId = tp.PatientId,
                Date = tp.Date,
                Diagnosis = tp.Diagnosis,
                TreatmentGoals = tp.TreatmentGoals,
                DietaryRecommendations = tp.DietaryRecommendations,
                ExerciseRecommendations = tp.ExerciseRecommendations,
                MedicationNotes = tp.MedicationNotes,
                FollowUpDate = tp.FollowUpDate,
                CreatedBy = tp.CreatedBy,
                CreatedAt = tp.CreatedAt,
                UpdatedAt = tp.UpdatedAt
            };
            return Ok(dto);
        }

        // GET: api/TreatmentPlan/patient/5
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<TreatmentPlanOutputDto>> GetTreatmentPlansByPatientId(int patientId)
        {
            var plans = _context.TreatmentPlans
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.Date)
                .Select(tp => new TreatmentPlanOutputDto
                {
                    Id = tp.Id,
                    PatientId = tp.PatientId,
                    Date = tp.Date,
                    Diagnosis = tp.Diagnosis,
                    TreatmentGoals = tp.TreatmentGoals,
                    DietaryRecommendations = tp.DietaryRecommendations,
                    ExerciseRecommendations = tp.ExerciseRecommendations,
                    MedicationNotes = tp.MedicationNotes,
                    FollowUpDate = tp.FollowUpDate,
                    CreatedBy = tp.CreatedBy,
                    CreatedAt = tp.CreatedAt,
                    UpdatedAt = tp.UpdatedAt
                })
                .ToList();
            return Ok(plans);
        }

        // POST: api/TreatmentPlan
        [HttpPost]
        public ActionResult<TreatmentPlanOutputDto> CreateTreatmentPlan([FromBody] TreatmentPlanDto dto)
        {
            if (dto == null)
                return BadRequest();
            if (dto.PatientId == 0 || string.IsNullOrEmpty(dto.Diagnosis) || string.IsNullOrEmpty(dto.TreatmentGoals))
                return BadRequest("PatientId, Diagnosis, and TreatmentGoals are required fields");

            DateTime? followUpDate = dto.FollowUpDate;
            if (dto.FollowUpDate == default || (dto.FollowUpDate is DateTime dt && dt == DateTime.MinValue))
                followUpDate = null;

            var treatmentPlan = new TreatmentPlan
            {
                PatientId = dto.PatientId,
                Date = dto.Date,
                Diagnosis = dto.Diagnosis,
                TreatmentGoals = dto.TreatmentGoals,
                DietaryRecommendations = dto.DietaryRecommendations,
                ExerciseRecommendations = dto.ExerciseRecommendations,
                MedicationNotes = dto.MedicationNotes,
                FollowUpDate = followUpDate ?? default,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };
            _context.TreatmentPlans.Add(treatmentPlan);
            _context.SaveChanges();
            _logger.LogInformation($"Created treatment plan {treatmentPlan.Id} for patient {treatmentPlan.PatientId}");

            var outputDto = new TreatmentPlanOutputDto
            {
                Id = treatmentPlan.Id,
                PatientId = treatmentPlan.PatientId,
                Date = treatmentPlan.Date,
                Diagnosis = treatmentPlan.Diagnosis,
                TreatmentGoals = treatmentPlan.TreatmentGoals,
                DietaryRecommendations = treatmentPlan.DietaryRecommendations,
                ExerciseRecommendations = treatmentPlan.ExerciseRecommendations,
                MedicationNotes = treatmentPlan.MedicationNotes,
                FollowUpDate = treatmentPlan.FollowUpDate,
                CreatedBy = treatmentPlan.CreatedBy,
                CreatedAt = treatmentPlan.CreatedAt,
                UpdatedAt = treatmentPlan.UpdatedAt
            };
            return CreatedAtAction(nameof(GetTreatmentPlanById), new { id = treatmentPlan.Id }, outputDto);
        }

        // PUT: api/TreatmentPlan/5
        [HttpPut("{id}")]
        public IActionResult UpdateTreatmentPlan(int id, TreatmentPlanDto dto)
        {
            if (dto == null)
                return BadRequest();
            var existingPlan = _context.TreatmentPlans.FirstOrDefault(p => p.Id == id);
            if (existingPlan == null)
                return NotFound();

            DateTime? followUpDate = dto.FollowUpDate;
            if (dto.FollowUpDate == default || (dto.FollowUpDate is DateTime dt && dt == DateTime.MinValue))
                followUpDate = null;

            existingPlan.PatientId = dto.PatientId;
            existingPlan.Date = dto.Date;
            existingPlan.Diagnosis = dto.Diagnosis;
            existingPlan.TreatmentGoals = dto.TreatmentGoals;
            existingPlan.DietaryRecommendations = dto.DietaryRecommendations;
            existingPlan.ExerciseRecommendations = dto.ExerciseRecommendations;
            existingPlan.MedicationNotes = dto.MedicationNotes;
            existingPlan.FollowUpDate = followUpDate ?? existingPlan.FollowUpDate;
            existingPlan.CreatedBy = dto.CreatedBy;
            existingPlan.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();
            _logger.LogInformation($"Updated treatment plan {id} for patient {existingPlan.PatientId}");
            return NoContent();
        }

        // DELETE: api/TreatmentPlan/5
        [HttpDelete("{id}")]
        public IActionResult DeleteTreatmentPlan(int id)
        {
            var plan = _context.TreatmentPlans.FirstOrDefault(p => p.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            _context.TreatmentPlans.Remove(plan);
            _context.SaveChanges();

            _logger.LogInformation($"Deleted treatment plan {id}");

            return NoContent();
        }

        // GET: api/TreatmentPlan/recommendations
        [HttpGet("recommendations")]
        public ActionResult<object> GetTreatmentRecommendations()
        {
            var recommendations = new
            {
                Dietary = new List<string>
                {
                    "Limit carbohydrate intake to 45-60g per meal",
                    "Avoid sugary drinks and desserts",
                    "Include more fiber-rich foods like vegetables, legumes, and whole grains",
                    "Choose low glycemic index foods",
                    "Monitor portion sizes carefully",
                    "Stay hydrated with water",
                    "Minimize processed foods"
                },
                Exercise = new List<string>
                {
                    "Aim for 150 minutes of moderate-intensity exercise per week",
                    "Include both aerobic exercises and resistance training",
                    "Monitor blood glucose before, during, and after exercise",
                    "Start slowly and increase intensity gradually",
                    "Consider walking for 10 minutes after meals to help control post-meal blood sugar",
                    "Stay hydrated during exercise",
                    "Incorporate daily movement breaks"
                }
            };

            return Ok(recommendations);
        }
    }
}
