using Labaratory.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Labaratory.DbContext
{
    public class ApplicationContext : IdentityDbContext<User>
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            //Database.EnsureCreated();
        }

        public DbSet<Patient>? Patients { get; set; }
        public DbSet<Application>? Applications { get; set; }
        public DbSet<AnalyzeCategory>? AnalyzeCategories { get; set; }
        public DbSet<AnalyzeType>? AnalyzeTypes { get; set; }
        public DbSet<Payments>? Payments { get; set; }
        public DbSet<Statistics>? Statistics { get; set; }
        public DbSet<PatientApplication>? PatientApplications { get; set; }
        public DbSet<Prescription>? Prescriptions { get; set; }
        public DbSet<AnalysisResult>? analysisResults { get; set; }
        public DbSet<AnalyzeSubItem>? AnalyzeSubItems { get; set; }
        public DbSet<SubItemResult>? SubItemResults { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity configuration
            modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(iul => new { iul.LoginProvider, iul.ProviderKey });
            modelBuilder.Entity<IdentityUserRole<string>>().HasKey(iur => new { iur.UserId, iur.RoleId });
            modelBuilder.Entity<IdentityUserToken<string>>().HasKey(iut => new { iut.UserId, iut.LoginProvider, iut.Name });

            // Application-specific configurations
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Applications)
                .HasForeignKey(a => a.PatientId);

            modelBuilder.Entity<AnalyzeType>()
                .HasOne(at => at.AnalyzeCategory)
                .WithMany(ac => ac.AnalyzeTypes)
                .HasForeignKey(at => at.AnalyzeCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payments>()
                .HasOne(p => p.Application)
                .WithOne()
                .HasForeignKey<Payments>(p => p.ApplicationId);

            modelBuilder.Entity<Statistics>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Statistics>()
                .Property(s => s.ExpenseName)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<Statistics>()
                .Property(s => s.Amount)
                .IsRequired();

            modelBuilder.Entity<Statistics>()
                .Property(s => s.AddDate)
                .IsRequired();

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.PatientApplication)
                .WithMany(pa => pa.Prescriptions)
                .HasForeignKey(p => p.PatientApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalyzeSubItem>()
                .HasOne(s => s.AnalyzeType)
                .WithMany()
                .HasForeignKey(s => s.AnalyzeTypeId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
