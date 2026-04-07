using AdminMembers.Services;
using AdminMembers.Middleware;
using AdminMembers.Data;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Identity;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Configure localization
builder.Services.AddLocalization();

var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("nl")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("nl");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// JSON options shared by Razor Pages and API controllers
var jsonOptions = new Action<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddRazorPages()
    .AddJsonOptions(jsonOptions)
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
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
    builder.Services.AddScoped<BlobStorageService>(_ => null!);
}

builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<StockExportService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<PasswordPolicyService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TotpService>();
builder.Services.AddScoped<MemberDocumentService>();
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

// Enable request localization
app.UseRequestLocalization();

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

// Temporary: expose detailed errors when SHOW_DETAILED_ERRORS=true env var is set
if (builder.Configuration.GetValue<bool>("SHOW_DETAILED_ERRORS"))
{
    app.UseDeveloperExceptionPage();
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
app.MapPost("/set-language", async (HttpContext httpContext) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var culture = form["culture"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var supportedCultureNames = supportedCultures.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var selectedCulture = supportedCultureNames.Contains(culture) ? culture : "nl";

    httpContext.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selectedCulture)),
        new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = false,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });

    return Results.LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});
app.MapGet("/", () => Results.Redirect("/Login"));

// Auto-apply EF migrations on startup — retry up to 5 times with backoff
using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrated = false;

    for (int attempt = 1; attempt <= 5; attempt++)
    {
        try
        {
            db.Database.Migrate();
            startupLogger.LogInformation("Database migrations applied successfully on attempt {Attempt}.", attempt);
            migrated = true;
            break;
        }
        catch (Exception ex) when (attempt < 5)
        {
            startupLogger.LogWarning(ex, "Migration attempt {Attempt} failed — retrying in {Delay}s.", attempt, attempt * 5);
            await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
        }
        catch (Exception ex)
        {
            startupLogger.LogCritical(ex, "All migration attempts failed. App will start but may be unstable.");
        }
    }

    if (!migrated)
        startupLogger.LogCritical("Migrations did not complete — check connection string and SQL firewall rules.");
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started in {Environment} mode", app.Environment.EnvironmentName);

app.Run();
