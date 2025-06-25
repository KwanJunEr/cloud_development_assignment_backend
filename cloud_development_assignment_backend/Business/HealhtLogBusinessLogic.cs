using cloud_development_assignment_backend.Models;

namespace cloud_development_assignment_backend.Business
{
    public class HealhtLogBusinessLogic
    {
        public string EvaluateStatus(HealthReading reading)
        {
           var statusList = new List<string>();

            // Blood Sugar checks
            if (reading.BloodSugar > 200)
                statusList.Add("Very High Blood Sugar");
            else if (reading.BloodSugar > 140)
                statusList.Add("High Blood Sugar");
            else if (reading.BloodSugar < 70)
                statusList.Add("Low Blood Sugar");
            else
                statusList.Add("Normal Blood Sugar!");

            // Blood Pressure checks
            if (reading.SystolicBP.HasValue && reading.DiastolicBP.HasValue)
            {
                if (reading.SystolicBP >= 140 || reading.DiastolicBP >= 90)
                    statusList.Add("High Blood Pressure");
                else
                    statusList.Add("Normal Blood Pressure");
            }
            else
            {
                statusList.Add("Blood Pressure Not Measured");
            }

            //Heart Rate Checks
            if (reading.HeartRate.HasValue)
            {
                if (reading.HeartRate > 100)
                    statusList.Add("High Heart Rate");
                else if (reading.HeartRate < 60)
                    statusList.Add("Low Heart Rate");
                else
                    statusList.Add("Normal Heart Rate");
            }
            else
            {
                statusList.Add("Heart Rate Not Measured");
            }
            return string.Join(", ", statusList);
        }
    }
}
