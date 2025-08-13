using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using NakitAkis.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Application Services
builder.Services.AddScoped<INakitAkisService, NakitAkisService>();
builder.Services.AddScoped<IExportService, ExportService>();

// HttpClient for API calls
builder.Services.AddHttpClient();

// Health Checks (sadece PostgreSQL)
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    }));
builder.Services.AddHangfireServer();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",  // Grafana
            "http://localhost:3001",  // React (primary)
            "http://localhost:3002",  // React backup port
            "https://localhost:3000",
            "https://localhost:3001",
            "https://localhost:3002"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// API Controllers
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Nakit Akış API",
        Version = "v1",
        Description = "Nakit Akış Dashboard API - Sadece Read-Only işlemler"
    });
});

var app = builder.Build();

// Database Connection Test (Development ortamında)
if (app.Environment.IsDevelopment())
{
    try
    {
        using var connection = new Npgsql.NpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        Console.WriteLine("✅ PostgreSQL connection successful!");
        await connection.CloseAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ PostgreSQL connection failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nakit Akış API V1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Nakit Akış API Documentation";
    });
}

app.UseHttpsRedirection();

// CORS Middleware
app.UseCors("AllowReactApp");

app.UseAuthorization();

// API Controllers
app.MapControllers();

// Welcome page for root path
app.MapGet("/", () => Results.Content($@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <title>NakitAkış API</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #007bff; }}
        .links {{ margin: 20px 0; }}
        .links a {{ display: block; margin: 10px 0; padding: 10px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; text-align: center; }}
        .links a:hover {{ background: #0056b3; }}
        .status {{ background: #d4edda; padding: 15px; border-radius: 4px; margin: 20px 0; }}
        code {{ background: #f8f9fa; padding: 10px; display: block; border-radius: 4px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🚀 NakitAkış API Server</h1>
        <div class='status'>
            ✅ <strong>API Server is running!</strong><br>
            🕐 Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
        </div>
        
        <h3>📋 Available Endpoints:</h3>
        <div class='links'>
            <a href='/swagger'>📖 Swagger UI - API Documentation</a>
            <a href='/health'>❤️ Health Check</a>
            <a href='/hangfire'>⚙️ Hangfire Dashboard</a>
            <a href='/api/grafana/variables/kaynak-kurulus'>🏢 Test API - Kaynak Kuruluş</a>
        </div>
        
        <h3>🔗 Frontend Applications:</h3>
        <div class='links'>
            <a href='http://localhost:3000/d/853acffc-686e-4bf1-882c-095f21c237ee/nakit-akis-analizi-dashboard?orgId=1&from=now-6M&to=now&timezone=browser&var-kaynak_kurulus=TARF%C4%B0N&var-fm_fonlar=&var-ihrac_no=' target='_blank'>📊 Grafana Dashboard</a>
            <a href='http://localhost:3001' target='_blank'>⚛️ React Frontend</a>
        </div>
        
        <h3>📊 Quick API Test:</h3>
        <p>Test your API with these URLs:</p>
        <code>
            GET /api/health<br>
            GET /api/grafana/variables/kaynak-kurulus<br>
            GET /api/grafana/analysis?faizOrani=15&kaynak_kurulus=FIBABANKA
        </code>
        
        <h3>🎯 Port Information:</h3>
        <ul>
            <li><strong>API Server:</strong> http://localhost:7289</li>
            <li><strong>React Frontend:</strong> http://localhost:3001</li>
            <li><strong>Grafana:</strong> http://localhost:3000</li>
        </ul>
    </div>
</body>
</html>", "text/html"));


// Health Checks
app.MapHealthChecks("/health");

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Nakit Akış Jobs"
});

// Startup Logs
Console.WriteLine("🚀 NakitAkis API Server Starting...");
Console.WriteLine("📡 API Base URL: http://localhost:7289");
Console.WriteLine("📖 Swagger UI: http://localhost:7289/swagger");
Console.WriteLine("⚙️  Hangfire Dashboard: http://localhost:7289/hangfire");
Console.WriteLine("❤️  Health Check: http://localhost:7289/health");
Console.WriteLine("🗄️  Database: PostgreSQL (Raw SQL)");
Console.WriteLine("🔗 CORS Enabled for:");
Console.WriteLine("   📊 Grafana: http://localhost:3000");
Console.WriteLine("   ⚛️  React: http://localhost:3001");
Console.WriteLine("   🔄 Backup: http://localhost:3002");
Console.WriteLine("⚡ Ready to serve React app!");

app.Run();

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}