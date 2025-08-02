using System;

namespace cloud_development_assignment_backend.Services
{
    public class MedicalSupplyStatusService
    {
        public string GetStatus(int quantity)
        {
            return quantity < 5 ? "Running Low" : "Sufficient";
        }
    }
}
