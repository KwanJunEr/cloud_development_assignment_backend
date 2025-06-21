namespace cloud_development_assignment_backend.Models
{
    public class HealthLog
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public DateTime LogDate { get; set; }
        public decimal? BloodSugarLevel { get; set; }
        public string InsulinDosage { get; set; }
        public string MealInformation { get; set; }
        public string Exercise { get; set; }
        public string Symptoms { get; set; }
        public string Notes { get; set; }
        public string RecordedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    //public class BloodSugarLog
    //{
    //    public string Id { get; set; }
    //    public string PatientId { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public double GlucoseLevel { get; set; }
    //    public string ReadingType { get; set; } // "fasting", "before meal", "after meal", "bedtime"
    //    public string Notes { get; set; }
    //}

    //public class InsulinLog
    //{
    //    public string Id { get; set; }
    //    public string PatientId { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public double Units { get; set; }
    //    public string InsulinType { get; set; }
    //    public string Notes { get; set; }
    //}

    //public class MealLog
    //{
    //    public string Id { get; set; }
    //    public string PatientId { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public string MealType { get; set; } // "breakfast", "lunch", "dinner", "snack"
    //    public string Description { get; set; }
    //    public double Carbohydrates { get; set; }
    //    public string Notes { get; set; }
    //}
}
