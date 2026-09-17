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

// Module 2: School & Student Management Services (75% Core)
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IStudentHistoryService, StudentHistoryService>();

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

// Seed initial roles, permissions, default SuperAdmin, sample school, and students
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
        "Module 2: School & Student Management (Active - 75% Core)"
    }
});

app.MapControllers();

app.Run();
