using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;
using RecruitmentSystem.Infrastructure.Repositories;
using RecruitmentSystem.Infrastructure.Services;
using RecruitmentSystem.Services.Implementations;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Services.Mappings;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RecruitmentSystem API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOi...\""
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<User, Role>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Account lockout policy
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// builder.Services.AddOptions<ResendClientOptions>()
//     .Configure<IConfiguration>((settings, configuration) =>
//     {
//         settings.ApiToken = configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend API Key is not configured");
//     });

builder.Services.AddHttpClient<IResend, ResendClient>();

// Email Service
// MailKit:
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

// Resend:
//builder.Services.AddScoped<IEmailService, EmailService>();

// AutoMapper configuration
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AuthenticationMappingProfile>();
    cfg.AddProfile<CandidateProfileMappingProfile>();
    cfg.AddProfile<StaffProfileMappingProfile>();
    cfg.AddProfile<JobPositionMappingProfile>();
    cfg.AddProfile<JobApplicationMappingProfile>();
    cfg.AddProfile<JobOfferMappingProfile>();
    cfg.AddProfile<InterviewMappingProfile>();
    cfg.AddProfile<SkillMappingProfile>();
});

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IS3Service, S3Service>();

// Repositories
builder.Services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
builder.Services.AddScoped<IStaffProfileRepository, StaffProfileRepository>();
builder.Services.AddScoped<IJobPositionRepository, JobPositionRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IUserManagementRepository, UserManagementRepository>();

// Job Application Management Repositories
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
builder.Services.AddScoped<IInterviewEvaluationRepository, InterviewEvaluationRepository>();
builder.Services.AddScoped<IInterviewParticipantRepository, InterviewParticipantRepository>();
builder.Services.AddScoped<IJobOfferRepository, JobOfferRepository>();
builder.Services.AddScoped<IApplicationStatusHistoryRepository, ApplicationStatusHistoryRepository>();

// Candidate Profile Services
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<ISkillService, SkillService>();

// Job Position Services
builder.Services.AddScoped<IJobPositionService, JobPositionService>();

// Staff Profile Services
builder.Services.AddScoped<IStaffProfileService, StaffProfileService>();

// User Management Services
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// Job Application Management Services
builder.Services.AddScoped<IJobApplicationManagementService, JobApplicationManagementService>();
builder.Services.AddScoped<IJobApplicationWorkflowService, JobApplicationWorkflowService>();
builder.Services.AddScoped<IJobApplicationAnalyticsService, JobApplicationAnalyticsService>();

// Job Offer Services
builder.Services.AddScoped<IJobOfferService, JobOfferService>();

// Interview Services - Required for interview scheduling functionality
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<IInterviewSchedulingService, InterviewSchedulingService>();
builder.Services.AddScoped<IInterviewEvaluationService, InterviewEvaluationService>();
builder.Services.AddScoped<IInterviewReportingService, InterviewReportingService>();
// Meeting Service for video conferencing integration
builder.Services.AddScoped<IMeetingService, JitsiMeetService>();
builder.Services.AddScoped<ISystemMaintenanceService, SystemMaintenanceService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthPolicy", context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 10,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    });

    options.AddPolicy("SubmissionPolicy", context =>
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = string.IsNullOrWhiteSpace(userId)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
            : userId;

        var isStaff = context.User?.IsInRole("Recruiter") == true ||
                      context.User?.IsInRole("HR") == true ||
                      context.User?.IsInRole("Admin") == true ||
                      context.User?.IsInRole("SuperAdmin") == true;

        var permitLimit = isStaff ? 40 : 20;
        var window = isStaff ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(1);
        var queueLimit = isStaff ? 10 : 2;

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        });
});

builder.Services.AddHangfireServer();

var automationSettings = builder.Configuration.GetSection("Automation");
var automationSystemUserId = automationSettings.GetValue<Guid?>("SystemUserId") ?? Guid.Empty;
var interviewReminderHours = Math.Max(automationSettings.GetValue<int?>("InterviewReminderHoursAhead") ?? 4, 1);
var evaluationReminderHours = Math.Max(automationSettings.GetValue<int?>("EvaluationReminderHoursAfter") ?? 24, 1);
var refreshTokenRetentionDays = Math.Max(automationSettings.GetValue<int?>("RefreshTokenRetentionDays") ?? 30, 1);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();

app.MapControllers();

RecurringJob.AddOrUpdate<ISystemMaintenanceService>(
    "expire-candidate-overrides",
    service => service.DisableExpiredCandidateOverridesAsync(CancellationToken.None),
    Cron.Daily());

RecurringJob.AddOrUpdate<ISystemMaintenanceService>(
    "close-expired-job-postings",
    service => service.CloseExpiredJobPostingsAsync(CancellationToken.None),
    Cron.Hourly());

RecurringJob.AddOrUpdate<ISystemMaintenanceService>(
    "purge-refresh-tokens",
    service => service.PurgeExpiredRefreshTokensAsync(refreshTokenRetentionDays, CancellationToken.None),
    Cron.Daily(hour: 2));

RecurringJob.AddOrUpdate<IJobOfferService>(
    "process-expired-offers",
    service => service.ProcessExpiredOffersAsync(automationSystemUserId),
    Cron.Hourly());

RecurringJob.AddOrUpdate<IJobOfferService>(
    "send-offer-expiry-reminders",
    service => service.SendBulkExpiryRemindersAsync(1),
    Cron.Daily());

RecurringJob.AddOrUpdate<IInterviewSchedulingService>(
    "upcoming-interview-reminders",
    service => service.SendUpcomingInterviewRemindersAsync(interviewReminderHours),
    Cron.Hourly());

RecurringJob.AddOrUpdate<IInterviewSchedulingService>(
    "evaluation-followups",
    service => service.SendPendingEvaluationRemindersAsync(evaluationReminderHours),
    Cron.Hourly());

app.Run();