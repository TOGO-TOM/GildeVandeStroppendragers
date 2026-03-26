using Microsoft.EntityFrameworkCore;
using AdminMembers.Data;
using AdminMembers.Services;
using AdminMembers.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext with SQL Server LocalDB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<AuthService>();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable static files for frontend
app.UseStaticFiles();

// Enable CORS
app.UseCors("AllowFrontend");

// Enable custom authentication middleware
app.UseAuthenticationMiddleware();

app.UseAuthorization();

app.MapControllers();

// Redirect root to login page
app.MapGet("/", () => Results.Redirect("/login.html"));

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting...");
logger.LogInformation("Navigate to: https://localhost:7223/login.html");

app.Run();
