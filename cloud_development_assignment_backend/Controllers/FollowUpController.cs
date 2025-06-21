using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class FollowUpController : ControllerBase
    {
        private readonly ILogger<FollowUpController> _logger;
        // KCG: In a real app, use repository or service here
        // Making this static AND public so it can be accessed from other controllers
        public static readonly List<FollowUp> _followUps = new List<FollowUp>();
        public static readonly List<Patient> _patients = new List<Patient>
        {
            new Patient { Id = "p1", Name = "John Doe", Age = 45, Gender = "Male", DiabetesType = "Type 2" },
            new Patient { Id = "p2", Name = "Mary Smith", Age = 38, Gender = "Female", DiabetesType = "Type 1" },
            new Patient { Id = "p3", Name = "Robert Johnson", Age = 62, Gender = "Male", DiabetesType = "Type 2" },
            new Patient { Id = "p4", Name = "Emily Davis", Age = 29, Gender = "Female", DiabetesType = "Type 1" },
            new Patient { Id = "p5", Name = "Michael Brown", Age = 52, Gender = "Male", DiabetesType = "Type 2" },
            new Patient { Id = "p6", Name = "Sarah Wilson", Age = 41, Gender = "Female", DiabetesType = "Type 2" }
        };

        public FollowUpController(ILogger<FollowUpController> logger)
        {
            _logger = logger;

            // Initialize with some data if empty
            // KCG: remove when using a real database
            if (!_followUps.Any())
            {
                _followUps.Add(new FollowUp
                {
                    Id = "fu1",
                    PatientId = "p1",
                    FlaggedDate = DateTime.Parse("2023-11-15"),
                    FlagReason = "Blood sugar consistently above 200 mg/dL for the last week",
                    FlaggedBy = "Dr. Sarah Johnson",
                    UrgencyLevel = "high",
                    Status = "pending"
                });

                _followUps.Add(new FollowUp
                {
                    Id = "fu2",
                    PatientId = "p2",
                    FlaggedDate = DateTime.Parse("2023-11-18"),
                    FlagReason = "Patient reported severe hypoglycemic episode",
                    FlaggedBy = "Dr. Sarah Johnson",
                    UrgencyLevel = "high",
                    Status = "scheduled",
                    FollowUpDate = DateTime.Parse("2023-11-25"),
                    FollowUpNotes = "Check blood glucose monitoring logs"
                });

                _followUps.Add(new FollowUp
                {
                    Id = "fu3",
                    PatientId = "p3",
                    FlaggedDate = DateTime.Parse("2023-11-10"),
                    FlagReason = "Non-compliance with medication regimen",
                    FlaggedBy = "Dr. Sarah Johnson",
                    UrgencyLevel = "medium",
                    Status = "pending"
                });
            }
        }

        // GET: api/followup
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllFollowUps()
        {
            var result = _followUps.Select(f => new
            {
                f.Id,
                f.PatientId,
                PatientName = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.Name ?? "Unknown",
                PatientAge = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.Age ?? 0,
                PatientGender = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.Gender ?? "Unknown",
                PatientCondition = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.DiabetesType ?? "Diabetes",
                f.FlaggedDate,
                f.FlagReason,
                f.FlaggedBy,
                f.UrgencyLevel,
                f.Status,
                f.FollowUpDate,
                f.FollowUpNotes
            }).ToList();

            return Ok(result);
        }

        // GET: api/followup/5
        [HttpGet("{id}")]
        public ActionResult<FollowUp> GetFollowUpById(string id)
        {
            var followUp = _followUps.FirstOrDefault(f => f.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            return Ok(followUp);
        }

        // GET: api/followup/patient/p1
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<object>> GetFollowUpsByPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
            {
                return BadRequest("Patient ID is required");
            }

            var patient = _patients.FirstOrDefault(p => p.Id == patientId);
            if (patient == null)
            {
                return NotFound($"Patient with ID {patientId} not found");
            }

            var followUps = _followUps
                .Where(f => f.PatientId == patientId)
                .Select(f => new
                {
                    f.Id,
                    f.PatientId,
                    PatientName = patient.Name,
                    f.FlaggedDate,
                    f.FlagReason,
                    f.FlaggedBy,
                    f.UrgencyLevel,
                    f.Status,
                    f.FollowUpDate,
                    f.FollowUpNotes
                })
                .ToList();

            if (!followUps.Any())
            {
                _logger.LogInformation($"No follow-ups found for patient {patientId}");
            }

            return Ok(followUps);
        }

        // GET: api/followup/status/pending
        [HttpGet("status/{status}")]
        public ActionResult<IEnumerable<object>> GetFollowUpsByStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("Status is required");
            }

            // Validate status (pending, scheduled, resolved)
            if (status != "pending" && status != "scheduled" && status != "resolved")
            {
                return BadRequest("Invalid status. Valid values are: pending, scheduled, resolved");
            }

            var followUps = _followUps
                .Where(f => f.Status.ToLower() == status.ToLower())
                .Select(f => new
                {
                    f.Id,
                    f.PatientId,
                    PatientName = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.Name ?? "Unknown",
                    f.FlaggedDate,
                    f.FlagReason,
                    f.FlaggedBy,
                    f.UrgencyLevel,
                    f.Status,
                    f.FollowUpDate,
                    f.FollowUpNotes
                })
                .ToList();

            _logger.LogInformation($"Found {followUps.Count} follow-ups with status '{status}'");

            return Ok(followUps);
        }

        // GET: api/followup/urgency/high
        [HttpGet("urgency/{urgencyLevel}")]
        public ActionResult<IEnumerable<object>> GetFollowUpsByUrgency(string urgencyLevel)
        {
            if (string.IsNullOrEmpty(urgencyLevel))
            {
                return BadRequest("Urgency level is required");
            }

            // Validate urgency level (low, medium, high)
            if (urgencyLevel != "low" && urgencyLevel != "medium" && urgencyLevel != "high")
            {
                return BadRequest("Invalid urgency level. Valid values are: low, medium, high");
            }

            var followUps = _followUps
                .Where(f => f.UrgencyLevel.ToLower() == urgencyLevel.ToLower())
                .Select(f => new
                {
                    f.Id,
                    f.PatientId,
                    PatientName = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.Name ?? "Unknown",
                    f.FlaggedDate,
                    f.FlagReason,
                    f.FlaggedBy,
                    f.UrgencyLevel,
                    f.Status,
                    f.FollowUpDate,
                    f.FollowUpNotes
                })
                .ToList();

            _logger.LogInformation($"Found {followUps.Count} follow-ups with urgency level '{urgencyLevel}'");

            return Ok(followUps);
        }

        // POST: api/followup
        [HttpPost]
        public ActionResult<FollowUp> CreateFollowUp(FollowUp followUp)
        {
            if (followUp == null)
            {
                return BadRequest("Follow-up data is required");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(followUp.PatientId))
            {
                return BadRequest("Patient ID is required");
            }

            if (string.IsNullOrEmpty(followUp.FlagReason))
            {
                return BadRequest("Flag reason is required");
            }

            if (string.IsNullOrEmpty(followUp.UrgencyLevel))
            {
                return BadRequest("Urgency level is required");
            }

            // Validate patient exists
            var patient = _patients.FirstOrDefault(p => p.Id == followUp.PatientId);
            if (patient == null)
            {
                return NotFound($"Patient with ID {followUp.PatientId} not found");
            }

            // Validate urgency level
            string urgency = followUp.UrgencyLevel.ToLower();
            if (urgency != "low" && urgency != "medium" && urgency != "high")
            {
                return BadRequest("Invalid urgency level. Valid values are: low, medium, high");
            }

            // Generate a new ID
            string newId = $"fu{_followUps.Count + 1}";

            // Set default values
            followUp.Id = newId;
            followUp.FlaggedDate = DateTime.Now;
            followUp.Status = followUp.Status ?? "pending";

            _followUps.Add(followUp);

            _logger.LogInformation($"Created new follow-up with ID {newId} for patient {followUp.PatientId}");

            return CreatedAtAction(nameof(GetFollowUpById), new { id = newId }, followUp);
        }

        // PUT: api/followup/5/status
        [HttpPut("{id}/status")]
        public IActionResult UpdateFollowUpStatus(string id, [FromBody] string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("Status is required");
            }

            var followUp = _followUps.FirstOrDefault(f => f.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            followUp.Status = status;
            _logger.LogInformation($"Updated follow-up {id} status to {status}");

            return NoContent();
        }

        // PUT: api/followup/5/schedule
        [HttpPut("{id}/schedule")]
        public IActionResult ScheduleFollowUp(string id, [FromBody] FollowUpSchedule schedule)
        {
            if (schedule == null || !schedule.FollowUpDate.HasValue)
            {
                return BadRequest("Follow-up date is required");
            }

            var followUp = _followUps.FirstOrDefault(f => f.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            followUp.FollowUpDate = schedule.FollowUpDate;
            followUp.FollowUpNotes = schedule.Notes;
            followUp.Status = "scheduled";

            _logger.LogInformation($"Scheduled follow-up for patient {followUp.PatientId} on {schedule.FollowUpDate.Value.ToShortDateString()}");

            return NoContent();
        }

        // PUT: api/followup/5/resolve
        [HttpPut("{id}/resolve")]
        public IActionResult MarkAsResolved(string id)
        {
            var followUp = _followUps.FirstOrDefault(f => f.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            followUp.Status = "resolved";
            _logger.LogInformation($"Marked follow-up {id} as resolved");

            return NoContent();
        }

        // GET: api/followup/search?term=john
        [HttpGet("search")]
        public ActionResult<IEnumerable<object>> SearchFollowUps([FromQuery] string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return BadRequest("Search term is required");
            }

            // Find patients matching the search term
            var matchingPatientIds = _patients
                .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Id)
                .ToList();

            // Get follow-ups for matching patients
            var followUps = _followUps
                .Where(f => matchingPatientIds.Contains(f.PatientId))
                .Select(f => new
                {
                    f.Id,
                    f.PatientId,
                    PatientName = _patients.FirstOrDefault(p => p.Id == f.PatientId)?.Name ?? "Unknown",
                    f.FlaggedDate,
                    f.FlagReason,
                    f.FlaggedBy,
                    f.UrgencyLevel,
                    f.Status,
                    f.FollowUpDate,
                    f.FollowUpNotes
                })
                .ToList();

            _logger.LogInformation($"Found {followUps.Count} follow-ups matching search term '{term}'");

            return Ok(followUps);
        }

        // GET: api/followup/patients
        [HttpGet("patients")]
        public ActionResult<IEnumerable<object>> GetAllPatientsForFollowUp()
        {
            var result = _patients.Select(p => new
            {
                p.Id,
                p.Name
            }).ToList();

            return Ok(result);
        }
    }

    // Helper class for scheduling follow-ups
    public class FollowUpSchedule
    {
        public DateTime? FollowUpDate { get; set; }
        public string Notes { get; set; }
    }
}
