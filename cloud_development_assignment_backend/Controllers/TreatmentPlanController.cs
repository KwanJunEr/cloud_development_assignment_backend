using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TreatmentPlanController : ControllerBase
    {
        private readonly ILogger<TreatmentPlanController> _logger;
        // KCG: use a repository or service here
        private static readonly List<TreatmentPlan> _treatmentPlans = new List<TreatmentPlan>();

        public TreatmentPlanController(ILogger<TreatmentPlanController> logger)
        {
            _logger = logger;

            // Initialize with sample data if empty
            // KCG: remove when real data ready
            if (!_treatmentPlans.Any())
            {
                // Sample data for John Doe (p1)
                _treatmentPlans.Add(new TreatmentPlan
                {
                    Id = "tp1",
                    PatientId = "p1",
                    Date = DateTime.Parse("2023-10-15"),
                    Diagnosis = "Type 2 Diabetes with poor glycemic control",
                    TreatmentGoals = "Improve HbA1c to <7% within 3 months",
                    DietaryRecommendations = "Low carbohydrate diet, limit to 45g per meal",
                    ExerciseRecommendations = "30 minutes of walking daily",
                    MedicationNotes = "Continue Metformin 500mg twice daily",
                    FollowUpDate = DateTime.Parse("2023-11-15"),
                    CreatedBy = "Dr. Smith",
                    CreatedAt = DateTime.Parse("2023-10-15")
                });

                // Sample data for Mary Smith (p2)
                _treatmentPlans.Add(new TreatmentPlan
                {
                    Id = "tp2",
                    PatientId = "p2",
                    Date = DateTime.Parse("2023-09-20"),
                    Diagnosis = "Type 1 Diabetes, well-controlled",
                    TreatmentGoals = "Maintain current glycemic control, prevent complications",
                    DietaryRecommendations = "Carbohydrate counting, 60g per meal",
                    ExerciseRecommendations = "Regular moderate exercise 5x weekly with proper insulin adjustment",
                    MedicationNotes = "Insulin regimen: Lantus 20 units at bedtime, Novolog with meals based on carb counting",
                    FollowUpDate = DateTime.Parse("2023-10-20"),
                    CreatedBy = "Dr. Johnson",
                    CreatedAt = DateTime.Parse("2023-09-20")
                });
            }
        }

        // GET: api/TreatmentPlan
        [HttpGet]
        public ActionResult<IEnumerable<TreatmentPlan>> GetAllTreatmentPlans()
        {
            return Ok(_treatmentPlans);
        }

        // GET: api/TreatmentPlan/5
        [HttpGet("{id}")]
        public ActionResult<TreatmentPlan> GetTreatmentPlanById(string id)
        {
            var treatmentPlan = _treatmentPlans.FirstOrDefault(p => p.Id == id);

            if (treatmentPlan == null)
            {
                return NotFound();
            }

            return Ok(treatmentPlan);
        }

        // GET: api/TreatmentPlan/patient/5
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<TreatmentPlan>> GetTreatmentPlansByPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return BadRequest("Patient ID is required");
            }

            var plans = _treatmentPlans
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.Date)
                .ToList();

            return Ok(plans);
        }

        // POST: api/TreatmentPlan
        [HttpPost]
        public ActionResult<TreatmentPlan> CreateTreatmentPlan(TreatmentPlan treatmentPlan)
        {
            if (treatmentPlan == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(treatmentPlan.PatientId) ||
                string.IsNullOrEmpty(treatmentPlan.Diagnosis) ||
                string.IsNullOrEmpty(treatmentPlan.TreatmentGoals))
            {
                return BadRequest("PatientId, Diagnosis, and TreatmentGoals are required fields");
            }

            if (string.IsNullOrEmpty(treatmentPlan.Id))
            {
                treatmentPlan.Id = Guid.NewGuid().ToString();
            }

            if (treatmentPlan.CreatedAt == default)
            {
                treatmentPlan.CreatedAt = DateTime.UtcNow;
            }

            if (treatmentPlan.Date == default)
            {
                treatmentPlan.Date = DateTime.UtcNow;
            }

            _treatmentPlans.Add(treatmentPlan);

            _logger.LogInformation($"Created treatment plan {treatmentPlan.Id} for patient {treatmentPlan.PatientId}");

            return CreatedAtAction(nameof(GetTreatmentPlanById), new { id = treatmentPlan.Id }, treatmentPlan);
        }

        // PUT: api/TreatmentPlan/5
        [HttpPut("{id}")]
        public IActionResult UpdateTreatmentPlan(string id, TreatmentPlan treatmentPlan)
        {
            if (treatmentPlan == null || id != treatmentPlan.Id)
            {
                return BadRequest();
            }

            var existingPlan = _treatmentPlans.FirstOrDefault(p => p.Id == id);

            if (existingPlan == null)
            {
                return NotFound();
            }

            treatmentPlan.UpdatedAt = DateTime.UtcNow;

            var index = _treatmentPlans.IndexOf(existingPlan);
            _treatmentPlans[index] = treatmentPlan;

            _logger.LogInformation($"Updated treatment plan {id} for patient {treatmentPlan.PatientId}");

            return NoContent();
        }

        // DELETE: api/TreatmentPlan/5
        [HttpDelete("{id}")]
        public IActionResult DeleteTreatmentPlan(string id)
        {
            var plan = _treatmentPlans.FirstOrDefault(p => p.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            _treatmentPlans.Remove(plan);

            _logger.LogInformation($"Deleted treatment plan {id}");

            return NoContent();
        }

        // GET: api/TreatmentPlan/recommendations
        [HttpGet("recommendations")]
        public ActionResult<object> GetTreatmentRecommendations()
        {
            // Standard recommendations for diabetes patients
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
