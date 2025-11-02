using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;
using RecruitmentSystem.Infrastructure.Services;
using RecruitmentSystem.Infrastructure.Repositories;
using RecruitmentSystem.Services.Implementations;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Services.Mappings;
using Resend;
using Microsoft.AspNetCore.Identity;

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

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

builder.Services.AddOptions<ResendClientOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        settings.ApiToken = configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend API Key is not configured");
    });

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
});

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IS3Service, S3Service>();

// Repositories
builder.Services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();
builder.Services.AddScoped<IStaffProfileRepository, StaffProfileRepository>();
builder.Services.AddScoped<IJobPositionRepository, JobPositionRepository>();

// Job Application Management Repositories
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
builder.Services.AddScoped<IInterviewEvaluationRepository, InterviewEvaluationRepository>();
builder.Services.AddScoped<IInterviewParticipantRepository, InterviewParticipantRepository>();
builder.Services.AddScoped<IJobOfferRepository, JobOfferRepository>();
builder.Services.AddScoped<IApplicationStatusHistoryRepository, ApplicationStatusHistoryRepository>();

// Candidate Profile Services
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();

// Job Position Services
builder.Services.AddScoped<IJobPositionService, JobPositionService>();

// Staff Profile Services
builder.Services.AddScoped<IStaffProfileService, StaffProfileService>();

// Job Application Management Services
builder.Services.AddScoped<IJobApplicationManagementService, JobApplicationManagementService>();
builder.Services.AddScoped<IJobApplicationWorkflowService, JobApplicationWorkflowService>();
builder.Services.AddScoped<IJobApplicationAnalyticsService, JobApplicationAnalyticsService>();
//builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
//builder.Services.AddScoped<IInterviewService, InterviewService>();
//builder.Services.AddScoped<IInterviewSchedulingService, InterviewSchedulingService>();
//builder.Services.AddScoped<IInterviewEvaluationService, InterviewEvaluationService>();
//builder.Services.AddScoped<IInterviewReportingService, InterviewReportingService>();
//builder.Services.AddScoped<IJobOfferService, JobOfferService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();