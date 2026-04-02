using AdminMembers.Services;
using AdminMembers.Middleware;
using AdminMembers.Data;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// JSON options shared by Razor Pages and API controllers
var jsonOptions = new Action<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddRazorPages().AddJsonOptions(jsonOptions);
builder.Services.AddControllers().AddJsonOptions(jsonOptions);
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            sql.CommandTimeout(30);
        });
});

// Configure Azure Blob Storage
var blobStorageEndpoint = builder.Configuration.GetValue<string>("AzureStorageBlob:Endpoint");
if (!string.IsNullOrEmpty(blobStorageEndpoint))
{
    builder.Services.AddSingleton(sp =>
    {
        return new BlobServiceClient(new Uri(blobStorageEndpoint), new DefaultAzureCredential());
    });
    builder.Services.AddScoped<BlobStorageService>();
}
else
{
    builder.Services.AddScoped<BlobStorageService?>(_ => null);
}

builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<PasswordPolicyService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TotpService>();
builder.Services.AddHostedService<AuditLogCleanupService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCaching();
app.UseSession();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthenticationMiddleware();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/Login"));

// Auto-apply EF migrations on startup (safe to run multiple times)
using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        startupLogger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(ex, "Database migration failed on startup. The application may not function correctly.");
        // Do not rethrow — let the app start so the error page is reachable
    }
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started in {Environment} mode", app.Environment.EnvironmentName);

app.Run();
