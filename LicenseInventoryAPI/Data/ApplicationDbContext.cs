using Microsoft.EntityFrameworkCore;
using LicenseInventoryAPI.Models;

namespace LicenseInventoryAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Tables in Database
        public DbSet<License> Licenses { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<SoftwareInstallation> SoftwareInstallations { get; set; }
        public DbSet<ComplianceEvent> ComplianceEvents { get; set; }
        public DbSet<CostAllocation> CostAllocations { get; set; }
        // NEW: Renewals Table
        public DbSet<Renewal> Renewals { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Device -> InstalledSoftware (One-to-Many)
            modelBuilder.Entity<SoftwareInstallation>()
                .HasOne(s => s.Device)
                .WithMany(d => d.InstalledSoftware)
                .HasForeignKey(s => s.DeviceId);

            // License -> Installations (One-to-Many, Optional)
            modelBuilder.Entity<SoftwareInstallation>()
                .HasOne(s => s.License)
                .WithMany(l => l.Installations)
                .HasForeignKey(s => s.LicenseId)
                .IsRequired(false);

            // License -> ComplianceEvents (One-to-Many)
            modelBuilder.Entity<ComplianceEvent>()
                .HasOne(c => c.License)
                .WithMany(l => l.ComplianceEvents)
                .HasForeignKey(c => c.LicenseId);

            // License -> CostAllocations (One-to-Many)
            modelBuilder.Entity<CostAllocation>()
                .HasOne(c => c.License)
                .WithMany(l => l.CostAllocations)
                .HasForeignKey(c => c.LicenseId);
        }
    }
}