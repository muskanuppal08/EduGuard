using EduGuard.Application.Interfaces;
using EduGuard.Infrastructure.Persistence;
using EduGuard.Infrastructure.Security;
using EduGuard.Infrastructure.Services;
using EduGuard.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Core Services
builder.Services.AddSingleton<IEduGuardDataStore, InMemoryEduGuardDataStore>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<DataSeeder>();

// Module 1: Auth & User Management Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Module 2: School & Student Management Services
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IStudentHistoryService, StudentHistoryService>();

// Module 3: Attendance Management Services
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceAnalyticsService, AttendanceAnalyticsService>();
builder.Services.AddScoped<IAbsencePatternDetector, AbsencePatternDetector>();

// Module 4: Academic Performance Services
builder.Services.AddScoped<IAcademicService, AcademicService>();
builder.Services.AddScoped<IMarksService, MarksService>();
builder.Services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();

// Configure CORS for web frontends
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Seed initial roles, permissions, default SuperAdmin, sample school, students, and attendance
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// Global Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable CORS
app.UseCors();

// JWT Authentication Middleware
app.UseMiddleware<JwtAuthenticationMiddleware>();

// Root Welcome & Health Endpoint
app.MapGet("/", () => new
{
    System = "EduGuard - Student Dropout Analysis & Retention Platform",
    Version = "1.0.0",
    Status = "Healthy",
    ActiveModules = new[]
    {
        "Module 1: Authentication & User Management (Active)",
        "Module 2: School & Student Management (Active)",
        "Module 3: Attendance Management (Active)",
        "Module 4: Academic Performance & Learning Loss Monitoring (Active)"
    }
});

app.MapControllers();

app.Run();
