using Microsoft.EntityFrameworkCore;
using cloud_development_assignment_backend.Models;

namespace cloud_development_assignment_backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<HealthReading> HealthReadings { get; set; }
        public DbSet<ProviderAvailability> ProviderAvailabilities { get; set; } 

        public DbSet<WordsofEncouragement> WordsofEncouragement { get; set; }

        public DbSet<FollowUp> FollowUps { get; set; }
        
        public DbSet<TreatmentPlan> TreatmentPlans { get; set; }

        public DbSet<Patient> PatientMedicalInfo { get; set; }

        public DbSet<Prescription> Prescriptions { get; set; }

        public DbSet<Medication> Medications { get; set; }

        public DbSet<MealEntry> MealEntries { get; set; }

        public DbSet<PhysicianNotification> PhysicianNotifications { get; set; }

        public DbSet<MedicationReminder> MedicationReminders { get; set; }

        public DbSet<PatientAppointmentBooking> PatientAppointmentBooking { get; set; }

    }
}