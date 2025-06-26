//using cloud_development_assignment_backend.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Cors;

//namespace cloud_development_assignment_backend.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class HealthLogController : ControllerBase
//    {
//        private readonly ILogger<HealthLogController> _logger;
//        // KCG: In a real app, use a repository or service here
//        private static readonly List<HealthLog> _healthLogs = new List<HealthLog>();

//        public HealthLogController(ILogger<HealthLogController> logger)
//        {
//            _logger = logger;

//            // Initialize with sample data if empty
//            // KCG: remove when using a real database
//            if (!_healthLogs.Any())
//            {
//                // Sample data for John Doe (p1)
//                _healthLogs.Add(new HealthLog
//                {
//                    Id = "hl1",
//                    PatientId = "p1",
//                    LogDate = DateTime.Parse("2023-11-20T08:00:00"),
//                    BloodSugarLevel = 130,
//                    InsulinDosage = "N/A",
//                    MealInformation = "Oatmeal with berries",
//                    Exercise = "Morning walk, 20 minutes",
//                    Symptoms = "None",
//                    Notes = "Feeling normal",
//                    RecordedBy = "Patient",
//                    CreatedAt = DateTime.Parse("2023-11-20T08:10:00")
//                });

//                _healthLogs.Add(new HealthLog
//                {
//                    Id = "hl2",
//                    PatientId = "p1",
//                    LogDate = DateTime.Parse("2023-11-19T22:00:00"),
//                    BloodSugarLevel = 145,
//                    InsulinDosage = "N/A",
//                    MealInformation = "Chicken with vegetables, had dessert after dinner",
//                    Exercise = "None",
//                    Symptoms = "None",
//                    Notes = "Had dessert after dinner",
//                    RecordedBy = "Patient",
//                    CreatedAt = DateTime.Parse("2023-11-19T22:15:00")
//                });

//                // Sample data for Mary Smith (p2)
//                _healthLogs.Add(new HealthLog
//                {
//                    Id = "hl3",
//                    PatientId = "p2",
//                    LogDate = DateTime.Parse("2023-11-20T08:00:00"),
//                    BloodSugarLevel = 175,
//                    InsulinDosage = "10 units Lantus",
//                    MealInformation = "Cereal with milk",
//                    Exercise = "None",
//                    Symptoms = "Mild headache",
//                    Notes = "Forgot evening insulin yesterday",
//                    RecordedBy = "Patient",
//                    CreatedAt = DateTime.Parse("2023-11-20T08:05:00")
//                });

//                _healthLogs.Add(new HealthLog
//                {
//                    Id = "hl4",
//                    PatientId = "p2",
//                    LogDate = DateTime.Parse("2023-11-19T14:00:00"),
//                    BloodSugarLevel = 160,
//                    InsulinDosage = "6 units Humalog",
//                    MealInformation = "Pasta with tomato sauce",
//                    Exercise = "None",
//                    Symptoms = "None",
//                    Notes = "Lunch was pasta",
//                    RecordedBy = "Patient",
//                    CreatedAt = DateTime.Parse("2023-11-19T14:10:00")
//                });

//                // Sample data for Robert Johnson (p3)
//                _healthLogs.Add(new HealthLog
//                {
//                    Id = "hl5",
//                    PatientId = "p3",
//                    LogDate = DateTime.Parse("2023-11-20T08:00:00"),
//                    BloodSugarLevel = 128,
//                    InsulinDosage = "N/A",
//                    MealInformation = "Whole grain toast with eggs",
//                    Exercise = "None",
//                    Symptoms = "None",
//                    Notes = "Normal morning reading",
//                    RecordedBy = "Patient",
//                    CreatedAt = DateTime.Parse("2023-11-20T08:15:00")
//                });

//                // Sample data for Emily Davis (p4)
//                _healthLogs.Add(new HealthLog
//                {
//                    Id = "hl6",
//                    PatientId = "p4",
//                    LogDate = DateTime.Parse("2023-11-20T08:00:00"),
//                    BloodSugarLevel = 140,
//                    InsulinDosage = "8 units Lantus",
//                    MealInformation = "Yogurt with fruit",
//                    Exercise = "None",
//                    Symptoms = "None",
//                    Notes = "Morning dose",
//                    RecordedBy = "Patient",
//                    CreatedAt = DateTime.Parse("2023-11-20T08:20:00")
//                });
//            }
//        }

//        // GET: api/HealthLog/patient/5
//        [HttpGet("patient/{patientId}")]
//        public ActionResult<IEnumerable<HealthLog>> GetHealthLogsByPatientId(string patientId)
//        {
//            if (string.IsNullOrEmpty(patientId))
//            {
//                return BadRequest("Patient ID is required");
//            }

//            var logs = _healthLogs
//                .Where(h => h.PatientId == patientId)
//                .OrderByDescending(h => h.LogDate)
//                .ToList();

//            return Ok(logs);
//        }

//        // GET: api/HealthLog/summary/5
//        [HttpGet("summary/{patientId}")]
//        public ActionResult<object> GetHealthLogSummaryByPatientId(string patientId)
//        {
//            if (string.IsNullOrEmpty(patientId))
//            {
//                return BadRequest("Patient ID is required");
//            }

//            var patientLogs = _healthLogs
//                .Where(h => h.PatientId == patientId)
//                .OrderByDescending(h => h.LogDate)
//                .ToList();

//            if (!patientLogs.Any())
//            {
//                return NotFound("No health logs found for this patient");
//            }

//            // Calculate summary statistics
//            var bloodSugarReadings = patientLogs.Where(l => l.BloodSugarLevel.HasValue).Select(l => l.BloodSugarLevel.Value).ToList();

//            var summary = new
//            {
//                PatientId = patientId,
//                AverageBloodSugar = bloodSugarReadings.Any() ? Math.Round(bloodSugarReadings.Average(), 1) : 0,
//                HighReadingsPercentage = bloodSugarReadings.Any() ? Math.Round((double)bloodSugarReadings.Count(bs => bs > 150) / bloodSugarReadings.Count * 100, 1) : 0,
//                LowReadingsPercentage = bloodSugarReadings.Any() ? Math.Round((double)bloodSugarReadings.Count(bs => bs < 70) / bloodSugarReadings.Count * 100, 1) : 0,
//                AverageInsulinDose = CalculateAverageInsulinDose(patientLogs),
//                AverageCarbsPerMeal = CalculateAverageCarbsPerMeal(patientLogs),
//                Trend = DetermineTrend(patientLogs),
//                LastUpdated = patientLogs.FirstOrDefault()?.LogDate.ToString("yyyy-MM-dd") ?? "N/A"
//            };

//            return Ok(summary);
//        }

//        // POST: api/HealthLog
//        [HttpPost]
//        public ActionResult<HealthLog> CreateHealthLog(HealthLog healthLog)
//        {
//            if (healthLog == null)
//            {
//                return BadRequest();
//            }

//            if (string.IsNullOrEmpty(healthLog.PatientId))
//            {
//                return BadRequest("PatientId is required");
//            }

//            if (string.IsNullOrEmpty(healthLog.Id))
//            {
//                healthLog.Id = Guid.NewGuid().ToString();
//            }

//            if (healthLog.LogDate == default)
//            {
//                healthLog.LogDate = DateTime.UtcNow;
//            }

//            if (healthLog.CreatedAt == default)
//            {
//                healthLog.CreatedAt = DateTime.UtcNow;
//            }

//            _healthLogs.Add(healthLog);

//            _logger.LogInformation($"Created health log {healthLog.Id} for patient {healthLog.PatientId}");

//            return CreatedAtAction(nameof(GetHealthLogById), new { id = healthLog.Id }, healthLog);
//        }

//        // GET: api/HealthLog/5
//        [HttpGet("{id}")]
//        public ActionResult<HealthLog> GetHealthLogById(string id)
//        {
//            var healthLog = _healthLogs.FirstOrDefault(h => h.Id == id);

//            if (healthLog == null)
//            {
//                return NotFound();
//            }

//            return Ok(healthLog);
//        }

//        // PUT: api/HealthLog/5
//        [HttpPut("{id}")]
//        public IActionResult UpdateHealthLog(string id, HealthLog healthLog)
//        {
//            if (healthLog == null || id != healthLog.Id)
//            {
//                return BadRequest();
//            }

//            var existingLog = _healthLogs.FirstOrDefault(h => h.Id == id);

//            if (existingLog == null)
//            {
//                return NotFound();
//            }

//            // Update health log
//            var index = _healthLogs.IndexOf(existingLog);
//            _healthLogs[index] = healthLog;

//            _logger.LogInformation($"Updated health log {id} for patient {healthLog.PatientId}");

//            return NoContent();
//        }

//        // DELETE: api/HealthLog/5
//        [HttpDelete("{id}")]
//        public IActionResult DeleteHealthLog(string id)
//        {
//            var healthLog = _healthLogs.FirstOrDefault(h => h.Id == id);

//            if (healthLog == null)
//            {
//                return NotFound();
//            }

//            _healthLogs.Remove(healthLog);

//            _logger.LogInformation($"Deleted health log {id}");

//            return NoContent();
//        }

//        // POST: api/HealthLog/flag
//        [HttpPost("flag")]
//        public ActionResult<FollowUp> FlagPatientForFollowUp(FlagPatientRequest request)
//        {
//            if (request == null || string.IsNullOrEmpty(request.PatientId) || string.IsNullOrEmpty(request.Reason))
//            {
//                return BadRequest("PatientId and Reason are required");
//            }

//            var followUp = new FollowUp
//            {
//                Id = Guid.NewGuid().ToString(),
//                PatientId = request.PatientId,
//                FlaggedDate = DateTime.UtcNow,
//                FlagReason = request.Reason,
//                FlaggedBy = request.FlaggedBy ?? "System",
//                UrgencyLevel = request.UrgencyLevel ?? "medium",
//                Status = "pending"
//            };

//            FollowUpController._followUps.Add(followUp);

//            _logger.LogInformation($"Flagged patient {request.PatientId} for follow-up with reason: {request.Reason}");

//            return CreatedAtAction("GetFollowUpById", "FollowUp", new { id = followUp.Id }, followUp);
//        }

//        // Helper methods for calculating summary statistics
//        private double CalculateAverageInsulinDose(List<HealthLog> logs)
//        {
//            var insulinDoses = new List<double>();

//            foreach (var log in logs)
//            {
//                if (!string.IsNullOrEmpty(log.InsulinDosage) && log.InsulinDosage != "N/A")
//                {
//                    var dosePart = log.InsulinDosage.Split(' ')[0];
//                    if (double.TryParse(dosePart, out double dose))
//                    {
//                        insulinDoses.Add(dose);
//                    }
//                }
//            }

//            return insulinDoses.Any() ? Math.Round(insulinDoses.Average(), 1) : 0;
//        }

//        private double CalculateAverageCarbsPerMeal(List<HealthLog> logs)
//        {
//            // This is a simplified estimation as the real data would likely
//            // come from a more structured meal logging system
//            return logs.Any() ? Math.Round(logs.Count * 15.5, 0) : 0;
//        }

//        private string DetermineTrend(List<HealthLog> logs)
//        {
//            if (logs.Count < 2) return "stable";

//            var orderedLogs = logs.OrderBy(l => l.LogDate).ToList();
//            var bloodSugarReadings = orderedLogs.Where(l => l.BloodSugarLevel.HasValue)
//                .Select(l => l.BloodSugarLevel.Value).ToList();

//            if (bloodSugarReadings.Count < 2) return "stable";

//            // Calculate a simple trend based on the first and last readings
//            var firstHalf = bloodSugarReadings.Take(bloodSugarReadings.Count / 2).Average();
//            var secondHalf = bloodSugarReadings.Skip(bloodSugarReadings.Count / 2).Average();

//            var difference = secondHalf - firstHalf;

//            if (difference < -10) return "improving";
//            if (difference > 10) return "worsening";
//            return "stable";
//        }
//    }

//    // Helper class for flagging patients
//    public class FlagPatientRequest
//    {
//        public string PatientId { get; set; }
//        public string Reason { get; set; }
//        public string FlaggedBy { get; set; }
//        public string UrgencyLevel { get; set; }
//    }
//}
