//using cloud_development_assignment_backend.Models;
//using Microsoft.AspNetCore.Mvc;

//namespace cloud_development_assignment_backend.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class DieticianController : Controller
//    {
//        private static List<DietTip> tips = new();
//        private static List<DietPlan> mealPlans = new();
//        private static List<DietAppointment> appointments = new();
//        private static List<Patient> patients = new();

//        static DieticianController()
//        {
//            tips = new List<DietTip>
//            {
//                new DietTip { Id = Guid.NewGuid().ToString(), Title = "Stay Hydrated", Content = "Drink at least 8 glasses of water daily.", CreatedBy = "Tom" },
//                new DietTip { Id = Guid.NewGuid().ToString(), Title = "Balanced Meals", Content = "Include protein, carbs, and healthy fats in each meal.", CreatedBy = "Tom" }
//            };

//            patients = new List<Patient>
//            {
//                new Patient
//                {
//                    Id = "p1",
//                    Name = "John Doe",
//                    Age = 45,
//                    Gender = "Male",
//                    Email = "john.doe@example.com",
//                    Phone = "555-123-4567",
//                    Address = "123 Main St",
//                    DiabetesType = "Type 2",
//                    DiagnosisDate = DateTime.Parse("2020-03-15"),
//                    LatestA1c = 7.1,
//                    EmergencyContact = "Jane Doe",
//                    Notes = "Active patient"
//                },
//                new Patient
//                {
//                    Id = "p2",
//                    Name = "Mary Smith",
//                    Age = 39,
//                    Gender = "Female",
//                    Email = "mary.smith@example.com",
//                    Phone = "555-987-6543",
//                    Address = "456 Side St",
//                    DiabetesType = "Type 1",
//                    DiagnosisDate = DateTime.Parse("2019-08-10"),
//                    LatestA1c = 6.8,
//                    EmergencyContact = "Mark Smith",
//                    Notes = "Requires weekly monitoring"
//                }
//            };

//            appointments = new List<DietAppointment>
//            {
//                new DietAppointment { Id = Guid.NewGuid().ToString(), PatientId = "p1", Date = DateTime.Today.AddHours(10), Status = "Booked", Notes = "Discuss carb control" },
//                new DietAppointment { Id = Guid.NewGuid().ToString(), PatientId = "", Date = DateTime.Today.AddHours(14), Status = "Available", Notes = "Open slot" }
//            };

//            mealPlans = new List<DietPlan>
//            {
//                new DietPlan
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    PatientId = "p1",
//                    Title = "Low Carb Plan",
//                    Description = "Focus on low glycemic index foods.",
//                    Recommendations = new List<string> { "Leafy greens", "Lean proteins", "Avoid sugar" },
//                    CreatedAt = DateTime.UtcNow
//                }
//            };
//        }

//        // GET: api/Dietician/tips
//        [HttpGet("tips")]
//        public IActionResult GetTips() => Ok(tips);

//        // POST: api/Dietician/tips
//        [HttpPost("tips")]
//        public IActionResult UploadTip([FromBody] DietTip tip)
//        {
//            tip.Id = Guid.NewGuid().ToString();
//            tips.Add(tip);
//            return Ok(tip);
//        }

//        // DELETE: api/Dietician/tips/1
//        [HttpDelete("tips/{id}")]
//        public IActionResult DeleteTip(string id)
//        {
//            var tip = tips.FirstOrDefault(t => t.Id == id);
//            if (tip == null) return NotFound();
//            tips.Remove(tip);
//            return NoContent();
//        }

//        // GET: api/Dietician/patients
//        [HttpGet("patients")]
//        public IActionResult GetPatients() => Ok(patients);

//        // GET: api/Dietician/appointments
//        [HttpGet("appointments")]
//        public IActionResult GetAppointments() => Ok(appointments);

//        // POST: api/Dietician/appointments
//        [HttpPost("appointments")]
//        public IActionResult CreateAppointment([FromBody] DietAppointment appointment)
//        {
//            appointment.Id = Guid.NewGuid().ToString();
//            appointments.Add(appointment);
//            return Ok(appointment);
//        }

//        // GET: api/Dietician/mealplans
//        [HttpGet("mealplans")]
//        public IActionResult GetMealPlans() => Ok(mealPlans);

//        // POST: api/Dietician/mealplans
//        [HttpPost("mealplans")]
//        public IActionResult CreateMealPlan([FromBody] DietPlan mealPlan)
//        {
//            mealPlan.Id = Guid.NewGuid().ToString();
//            mealPlans.Add(mealPlan);
//            return Ok(mealPlan);
//        }

//        [HttpPut("mealplans/{id}")]
//        public IActionResult UpdateMealPlan(string id, [FromBody] DietPlan updatedPlan)
//        {
//            var plan = mealPlans.FirstOrDefault(mp => mp.Id == id);
//            if (plan == null) return NotFound();
//            plan.Title = updatedPlan.Title;
//            plan.Description = updatedPlan.Description;
//            plan.Recommendations = updatedPlan.Recommendations;
//            return Ok(plan);
//        }

//        [HttpDelete("mealplans/{id}")]
//        public IActionResult DeleteMealPlan(string id)
//        {
//            var plan = mealPlans.FirstOrDefault(mp => mp.Id == id);
//            if (plan == null) return NotFound();
//            mealPlans.Remove(plan);
//            return NoContent();
//        }
//    }
//}
