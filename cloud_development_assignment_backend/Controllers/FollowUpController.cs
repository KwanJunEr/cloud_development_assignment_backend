using cloud_development_assignment_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using cloud_development_assignment_backend.Data;
using cloud_development_assignment_backend.DTO;

namespace cloud_development_assignment_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class FollowUpController : ControllerBase
    {
        private readonly ILogger<FollowUpController> _logger;
        private readonly AppDbContext _context;

        public FollowUpController(AppDbContext context, ILogger<FollowUpController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/followup
        [HttpGet]
        public ActionResult<IEnumerable<FollowUpOutputDto>> GetAllFollowUps()
        {
            var followUps = _context.FollowUps.ToList();
            var result = followUps.Select(f =>
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == f.PatientId);
                return new FollowUpOutputDto
                {
                    Id = f.Id,
                    PatientId = f.PatientId,
                    PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                    FlaggedDate = f.FlaggedDate,
                    FlagReason = f.FlagReason ?? "",
                    FlaggedBy = f.FlaggedBy ?? "",
                    UrgencyLevel = f.UrgencyLevel ?? "",
                    Status = f.Status ?? "",
                    FollowUpDate = f.FollowUpDate,
                    FollowUpNotes = f.FollowUpNotes ?? ""
                };
            }).ToList();

            return Ok(result);
        }

        
        // GET: api/followup/{id}
        [HttpGet("{id}")]
        public ActionResult<FollowUpOutputDto> GetFollowUpById(int id)
        {
            var followUp = _context.FollowUps.FirstOrDefault(f => f.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == followUp.PatientId);
            var dto = new FollowUpOutputDto
            {
                Id = followUp.Id,
                PatientId = followUp.PatientId,
                PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                FlaggedDate = followUp.FlaggedDate,
                FlagReason = followUp.FlagReason ?? "",
                FlaggedBy = followUp.FlaggedBy ?? "",
                UrgencyLevel = followUp.UrgencyLevel ?? "",
                Status = followUp.Status ?? "",
                FollowUpDate = followUp.FollowUpDate,
                FollowUpNotes = followUp.FollowUpNotes ?? ""
            };

            return Ok(dto);
        }

        // GET: api/followup/patient/{patientId}
        [HttpGet("patient/{patientId}")]
        public ActionResult<IEnumerable<FollowUpOutputDto>> GetFollowUpsByPatientId(int patientId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == patientId);
            if (user == null)
            {
                return NotFound($"Patient with ID {patientId} not found");
            }

            var followUps = _context.FollowUps
                .Where(f => f.PatientId == patientId)
                .Select(f => new FollowUpOutputDto
                {
                    Id = f.Id,
                    PatientId = f.PatientId,
                    PatientName = $"{user.FirstName} {user.LastName}",
                    FlaggedDate = f.FlaggedDate,
                    FlagReason = f.FlagReason ?? "",
                    FlaggedBy = f.FlaggedBy ?? "",
                    UrgencyLevel = f.UrgencyLevel ?? "",
                    Status = f.Status ?? "",
                    FollowUpDate = f.FollowUpDate,
                    FollowUpNotes = f.FollowUpNotes ?? ""
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
        public ActionResult<IEnumerable<FollowUpOutputDto>> GetFollowUpsByStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("Status is required");
            }

            if (status != "pending" && status != "scheduled" && status != "resolved")
            {
                return BadRequest("Invalid status. Valid values are: pending, scheduled, resolved");
            }

            var followUps = _context.FollowUps
             .Where(f => f.Status.ToLower() == status.ToLower())
             .ToList() 
             .Select(f => {
                 var user = _context.Users.FirstOrDefault(u => u.Id == f.PatientId);
                 return new FollowUpOutputDto
                 {
                     Id = f.Id,
                     PatientId = f.PatientId,
                     PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                     FlaggedDate = f.FlaggedDate,
                     FlagReason = f.FlagReason ?? "",
                     FlaggedBy = f.FlaggedBy ?? "",
                     UrgencyLevel = f.UrgencyLevel ?? "",
                     Status = f.Status ?? "",
                     FollowUpDate = f.FollowUpDate,
                     FollowUpNotes = f.FollowUpNotes ?? ""
                 };
             })
             .ToList();

            _logger.LogInformation($"Found {followUps.Count} follow-ups with status '{status}'");

            return Ok(followUps);
        }

        // GET: api/followup/urgency/high
        [HttpGet("urgency/{urgencyLevel}")]
        public ActionResult<IEnumerable<FollowUpOutputDto>> GetFollowUpsByUrgency(string urgencyLevel)
        {
            if (string.IsNullOrEmpty(urgencyLevel))
            {
                return BadRequest("Urgency level is required");
            }

            if (urgencyLevel != "low" && urgencyLevel != "medium" && urgencyLevel != "high")
            {
                return BadRequest("Invalid urgency level. Valid values are: low, medium, high");
            }

            var followUps = _context.FollowUps
                .Where(f => f.UrgencyLevel.ToLower() == urgencyLevel.ToLower())
                .ToList() 
                .Select(f => {
                    var user = _context.Users.FirstOrDefault(u => u.Id == f.PatientId);
                    return new FollowUpOutputDto
                    {
                        Id = f.Id,
                        PatientId = f.PatientId,
                        PatientName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                        FlaggedDate = f.FlaggedDate,
                        FlagReason = f.FlagReason ?? "",
                        FlaggedBy = f.FlaggedBy ?? "",
                        UrgencyLevel = f.UrgencyLevel ?? "",
                        Status = f.Status ?? "",
                        FollowUpDate = f.FollowUpDate,
                        FollowUpNotes = f.FollowUpNotes ?? ""
                    };
                })
                .ToList();

            _logger.LogInformation($"Found {followUps.Count} follow-ups with urgency level '{urgencyLevel}'");

            return Ok(followUps);
        }

        // POST: api/followup
        [HttpPost]
        public ActionResult<FollowUpOutputDto> CreateFollowUp(FollowUpDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Follow-up data is required");
            }

            if (dto.PatientId == 0)
            {
                return BadRequest("Patient ID is required");
            }

            if (string.IsNullOrEmpty(dto.FlagReason))
            {
                return BadRequest("Flag reason is required");
            }

            if (string.IsNullOrEmpty(dto.UrgencyLevel))
            {
                return BadRequest("Urgency level is required");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == dto.PatientId);
            if (user == null)
            {
                return NotFound($"Patient with ID {dto.PatientId} not found");
            }

            string urgency = dto.UrgencyLevel.ToLower();
            if (urgency != "low" && urgency != "medium" && urgency != "high")
            {
                return BadRequest("Invalid urgency level. Valid values are: low, medium, high");
            }

            var followUp = new FollowUp
            {
                PatientId = dto.PatientId,
                FlaggedDate = dto.FlaggedDate != default ? dto.FlaggedDate : DateTime.Now,
                FlagReason = dto.FlagReason,
                FlaggedBy = dto.FlaggedBy,
                UrgencyLevel = dto.UrgencyLevel,
                Status = dto.Status ?? "pending",
                FollowUpDate = dto.FollowUpDate,
                FollowUpNotes = dto.FollowUpNotes
            };

            _context.FollowUps.Add(followUp);
            _context.SaveChanges();

            _logger.LogInformation($"Created new follow-up for patient {followUp.PatientId}");

            var outputDto = new FollowUpOutputDto
            {
                Id = followUp.Id,
                PatientId = followUp.PatientId,
                PatientName = $"{user.FirstName} {user.LastName}",
                FlaggedDate = followUp.FlaggedDate,
                FlagReason = followUp.FlagReason ?? "",
                FlaggedBy = followUp.FlaggedBy ?? "",
                UrgencyLevel = followUp.UrgencyLevel ?? "",
                Status = followUp.Status ?? "",
                FollowUpDate = followUp.FollowUpDate,
                FollowUpNotes = followUp.FollowUpNotes ?? ""
            };

            return CreatedAtAction(nameof(GetFollowUpById), new { id = followUp.Id }, outputDto);
        }

        // PUT: api/followup/5/status
        [HttpPut("{id}/status")]
        public IActionResult UpdateFollowUpStatus(string id, [FromBody] string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return BadRequest("Status is required");
            }

            var followUp = _context.FollowUps.FirstOrDefault(f => f.Id.ToString() == id);

            if (followUp == null)
            {
                return NotFound();
            }

            followUp.Status = status;
            _context.SaveChanges();
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

            var followUp = _context.FollowUps.FirstOrDefault(f => f.Id.ToString() == id);

            if (followUp == null)
            {
                return NotFound();
            }

            followUp.FollowUpDate = schedule.FollowUpDate;
            followUp.FollowUpNotes = schedule.Notes;
            followUp.Status = "scheduled";

            _context.SaveChanges();
            _logger.LogInformation($"Scheduled follow-up for patient {followUp.PatientId} on {schedule.FollowUpDate.Value.ToShortDateString()}");

            return NoContent();
        }

        // PUT: api/followup/5/resolve
        [HttpPut("{id}/resolve")]
        public IActionResult MarkAsResolved(string id)
        {
            var followUp = _context.FollowUps.FirstOrDefault(f => f.Id.ToString() == id);

            if (followUp == null)
            {
                return NotFound();
            }

            followUp.Status = "resolved";
            _context.SaveChanges();
            _logger.LogInformation($"Marked follow-up {id} as resolved");

            return NoContent();
        }

        // GET: api/followup/patients
        [HttpGet("patients")]
        public ActionResult<IEnumerable<object>> GetAllPatientsForFollowUp()
        {
            var result = _context.Users
                .Where(u => u.Role == "Patient")
                .Select(u => new
                {
                    Id = u.Id.ToString(),
                    Name = u.FirstName + " " + u.LastName
                }).ToList();

            return Ok(result);
        }
    }

    public class FollowUpSchedule
    {
        public DateTime? FollowUpDate { get; set; }
        public string Notes { get; set; }
    }
}
