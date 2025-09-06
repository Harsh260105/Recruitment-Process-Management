using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>,
        UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Custom DbSets
        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<StaffProfile> StaffProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Identity tables
            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<UserRole>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            // Configure UserRole relationship
            builder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
            });

            // Configure CandidateProfile relationships
            builder.Entity<CandidateProfile>(entity =>
            {
                entity.HasOne(cp => cp.User)
                    .WithOne(u => u.CandidateProfile)
                    .HasForeignKey<CandidateProfile>(cp => cp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cp => cp.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(cp => cp.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure StaffProfile relationships
            builder.Entity<StaffProfile>(entity =>
            {
                entity.HasOne(sp => sp.User)
                    .WithOne(u => u.StaffProfile)
                    .HasForeignKey<StaffProfile>(sp => sp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sp => sp.ReportingManager)
                    .WithMany(u => u.ManagedStaff)
                    .HasForeignKey(sp => sp.ReportingManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed default roles
            SeedRoles(builder);
        }

        private void SeedRoles(ModelBuilder builder)
        {
            var roles = new[]
            {
                new Role { Id = new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"), Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "System Super Administrator" },
                new Role { Id = new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"), Name = "Admin", NormalizedName = "ADMIN", Description = "System Administrator" },
                new Role { Id = new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"), Name = "Recruiter", NormalizedName = "RECRUITER", Description = "Recruiter" },
                new Role { Id = new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"), Name = "HR", NormalizedName = "HR", Description = "Human Resources" },
                new Role { Id = new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"), Name = "Interviewer", NormalizedName = "INTERVIEWER", Description = "Interviewer" },
                new Role { Id = new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"), Name = "Reviewer", NormalizedName = "REVIEWER", Description = "CV Reviewer" },
                new Role { Id = new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"), Name = "Candidate", NormalizedName = "CANDIDATE", Description = "Job Candidate" },
                new Role { Id = new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"), Name = "Viewer", NormalizedName = "VIEWER", Description = "Read-only Viewer" }
            };

            builder.Entity<Role>().HasData(roles);
        }
    }
}