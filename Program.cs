using AdminMembers.Data;
using AdminMembers.Services;
using AdminMembers.Middleware;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add memory cache for improved performance
builder.Services.AddMemoryCache();

// Add response caching
builder.Services.AddResponseCaching();

// Configure JSON serialization options once
var jsonOptions = new Action<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve casing for performance
});

// Add services to the container.
builder.Services.AddRazorPages().AddJsonOptions(jsonOptions);
builder.Services.AddControllers().AddJsonOptions(jsonOptions);

// Swagger only in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// Add DbContext with SQL Server - with connection pooling
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30); // 30 seconds timeout
        });
    
    // Optimize for production
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }
});

// Register services as scoped (one instance per request)
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<AuthService>();

// Add session support with distributed cache for production scalability
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // HTTPS only
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict; // CSRF protection
});

// CORS - tighten in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            // In production, specify exact origins
            policy.WithOrigins("https://yourdomain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

// Enable response compression (must be before static files)
app.UseResponseCompression();

app.UseHttpsRedirection();

// Enable static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year in production
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
        }
    }
});

// Enable response caching
app.UseResponseCaching();

// Enable session
app.UseSession();

// Enable routing
app.UseRouting();

// Enable CORS
app.UseCors("AllowFrontend");

// Enable custom authentication middleware
app.UseAuthenticationMiddleware();

app.UseAuthorization();

// Map Razor Pages
app.MapRazorPages();

// Map API Controllers
app.MapControllers();

// Redirect root to login page
app.MapGet("/", () => Results.Redirect("/Login"));

// Legacy route redirects (backward compatibility)
app.MapGet("/login.html", () => Results.Redirect("/Login"));
app.MapGet("/home.html", () => Results.Redirect("/Home"));
app.MapGet("/members.html", () => Results.Redirect("/Members"));
app.MapGet("/settings.html", () => Results.Redirect("/Settings"));
app.MapGet("/export.html", () => Results.Redirect("/Home"));

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting in {Environment} mode", app.Environment.EnvironmentName);
logger.LogInformation("Navigate to: https://localhost:7223/Login");

app.Run();
