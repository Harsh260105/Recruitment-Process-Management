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

        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<StaffProfile> StaffProfiles { get; set; }
        public DbSet<CandidateSkill> CandidateSkills { get; set; }
        public DbSet<CandidateEducation> CandidateEducations { get; set; }
        public DbSet<CandidateWorkExperience> CandidateWorkExperiences { get; set; }
        public DbSet<Skill> Skills { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<UserRole>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

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

            builder.Entity<CandidateSkill>(entity =>
            {
                entity.HasKey(cs => cs.Id);

                entity.HasOne(cs => cs.CandidateProfile)
                    .WithMany(cp => cp.CandidateSkills)
                    .HasForeignKey(cs => cs.CandidateProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Skill)
                    .WithMany(s => s.CandidateSkills)
                    .HasForeignKey(cs => cs.SkillId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(cs => new { cs.CandidateProfileId, cs.SkillId })
                    .IsUnique();
            });

            builder.Entity<CandidateEducation>(entity =>
            {
                entity.HasKey(ce => ce.Id);

                entity.HasOne(ce => ce.CandidateProfile)
                    .WithMany(cp => cp.CandidateEducations)
                    .HasForeignKey(ce => ce.CandidateProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CandidateWorkExperience>(entity =>
            {
                entity.HasKey(cwe => cwe.Id);

                entity.HasOne(cwe => cwe.CandidateProfile)
                    .WithMany(cp => cp.CandidateWorkExperiences)
                    .HasForeignKey(cwe => cwe.CandidateProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            SeedRoles(builder);
            SeedSkills(builder);
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

        private void SeedSkills(ModelBuilder builder)
        {
            var skills = new[]
            {
                // Programming Languages
                new Skill { Id = 1, Name = "C#", Category = "Programming Languages", Description = "Modern object-oriented programming language", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 2, Name = "Java", Category = "Programming Languages", Description = "Popular enterprise programming language", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 3, Name = "Python", Category = "Programming Languages", Description = "Versatile programming language for data science and web development", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 4, Name = "JavaScript", Category = "Programming Languages", Description = "Client-side scripting language for web development", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 5, Name = "TypeScript", Category = "Programming Languages", Description = "Typed superset of JavaScript", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },

                // Web Technologies
                new Skill { Id = 6, Name = "React", Category = "Web Technologies", Description = "JavaScript library for building user interfaces", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 7, Name = "Angular", Category = "Web Technologies", Description = "Platform for building mobile and desktop web applications", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 8, Name = "Node.js", Category = "Web Technologies", Description = "JavaScript runtime built on Chrome's V8 JavaScript engine", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 9, Name = "ASP.NET Core", Category = "Web Technologies", Description = "Cross-platform framework for building modern web applications", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },

                // Databases
                new Skill { Id = 10, Name = "SQL Server", Category = "Databases", Description = "Microsoft's relational database management system", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 11, Name = "MySQL", Category = "Databases", Description = "Open-source relational database management system", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 12, Name = "PostgreSQL", Category = "Databases", Description = "Advanced open-source relational database", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 13, Name = "MongoDB", Category = "Databases", Description = "NoSQL document database", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },

                // Cloud & DevOps
                new Skill { Id = 14, Name = "AWS", Category = "Cloud & DevOps", Description = "Amazon Web Services cloud platform", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 15, Name = "Azure", Category = "Cloud & DevOps", Description = "Microsoft's cloud computing platform", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 16, Name = "Docker", Category = "Cloud & DevOps", Description = "Platform for developing, shipping, and running applications", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 17, Name = "Git", Category = "Cloud & DevOps", Description = "Distributed version control system", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },

                // Soft Skills
                new Skill { Id = 18, Name = "Communication", Category = "Soft Skills", Description = "Ability to convey information effectively", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 19, Name = "Teamwork", Category = "Soft Skills", Description = "Ability to work effectively with others", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) },
                new Skill { Id = 20, Name = "Problem Solving", Category = "Soft Skills", Description = "Ability to identify and resolve problems", CreatedAt = new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc) }
            };

            builder.Entity<Skill>().HasData(skills);
        }
    }
}