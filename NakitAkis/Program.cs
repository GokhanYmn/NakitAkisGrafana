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

// Welcome page for root path - UTF-8 Türkçe Destekli
app.MapGet("/", () => Results.Text($@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>NakitAkış API - Ana Sayfa</title>
    <style>
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            margin: 40px; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
        }}
        .container {{ 
            max-width: 700px; 
            margin: 0 auto; 
            background: white; 
            padding: 30px; 
            border-radius: 12px; 
            box-shadow: 0 8px 32px rgba(0,0,0,0.1);
        }}
        h1 {{ 
            color: #2c3e50; 
            text-align: center;
            margin-bottom: 10px;
        }}
        .subtitle {{
            text-align: center;
            color: #7f8c8d;
            margin-bottom: 30px;
            font-style: italic;
        }}
        .links {{ margin: 20px 0; }}
        .links a {{ 
            display: block; 
            margin: 8px 0; 
            padding: 12px; 
            background: linear-gradient(45deg, #3498db, #2980b9); 
            color: white; 
            text-decoration: none; 
            border-radius: 6px; 
            text-align: center;
            transition: transform 0.2s ease;
        }}
        .links a:hover {{ 
            transform: translateY(-2px);
            box-shadow: 0 4px 15px rgba(52, 152, 219, 0.3);
        }}
        .status {{ 
            background: linear-gradient(45deg, #2ecc71, #27ae60); 
            padding: 15px; 
            border-radius: 8px; 
            margin: 20px 0;
            color: white;
            text-align: center;
        }}
        .section {{
            margin: 25px 0;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 8px;
            border-left: 4px solid #3498db;
        }}
        code {{ 
            background: #2c3e50; 
            color: #ecf0f1;
            padding: 15px; 
            display: block; 
            border-radius: 6px; 
            margin: 10px 0;
            font-family: 'Courier New', monospace;
            white-space: pre-line;
        }}
        .info-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
            margin: 20px 0;
        }}
        .info-card {{
            background: #ecf0f1;
            padding: 15px;
            border-radius: 6px;
            text-align: center;
        }}
        .emoji {{
            font-size: 1.2em;
            margin-right: 8px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🚀 NakitAkış API Sunucusu</h1>
        <p class='subtitle'>Nakit Akış Analizi ve Dashboard API Sistemi</p>
        
        <div class='status'>
            <span class='emoji'>✅</span><strong>API Sunucusu Çalışıyor!</strong><br>
            <span class='emoji'>🕐</span>Başlatılma Zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}<br>
            <span class='emoji'>🗄️</span>Veritabanı: PostgreSQL Bağlantısı Aktif
        </div>
        
        <div class='section'>
            <h3><span class='emoji'>📋</span>Mevcut API Endpoints:</h3>
            <div class='links'>
                <a href='/swagger'><span class='emoji'>📖</span>Swagger UI - API Dokümantasyonu</a>
                <a href='/health'><span class='emoji'>❤️</span>Sistem Sağlık Kontrolü</a>
                <a href='/hangfire'><span class='emoji'>⚙️</span>Hangfire Job Dashboard</a>
                <a href='/api/grafana/variables/kaynak-kurulus'><span class='emoji'>🏢</span>Test API - Kaynak Kuruluş Listesi</a>
            </div>
        </div>
        
        <div class='section'>
            <h3><span class='emoji'>🔗</span>Frontend Uygulamaları:</h3>
            <div class='links'>
                <a href='http://localhost:3000/d/853acffc-686e-4bf1-882c-095f21c237ee/nakit-akis-analizi-dashboard?orgId=1&from=now-6M&to=now&timezone=browser&var-kaynak_kurulus=&var-fm_fonlar=&var-ihrac_no=' target='_blank'><span class='emoji'>📊</span>Grafana Dashboard</a>
                <a href='http://localhost:3001' target='_blank'><span class='emoji'>⚛️</span>React Frontend Uygulaması</a>
            </div>
        </div>
        
        <div class='section'>
            <h3><span class='emoji'>📊</span>API Test Örnekleri:</h3>
            <p>Bu URL'lerle API'nizi test edebilirsiniz:</p>
            <code>GET /api/health
GET /api/grafana/variables/kaynak-kurulus  
GET /api/grafana/variables/fonlar?kaynak_kurulus=FIBABANKA
GET /api/grafana/analysis?faizOrani=15&kaynak_kurulus=FIBABANKA</code>
        </div>
        
        <div class='section'>
            <h3><span class='emoji'>🎯</span>Port Bilgileri:</h3>
            <div class='info-grid'>
                <div class='info-card'>
                    <strong>🔌 API Sunucusu</strong><br>
                    <code style='background: #3498db; color: white; padding: 5px; border-radius: 3px;'>http://localhost:7289</code>
                </div>
                <div class='info-card'>
                    <strong>⚛️ React Frontend</strong><br>
                    <code style='background: #e74c3c; color: white; padding: 5px; border-radius: 3px;'>http://localhost:3001</code>
                </div>
                <div class='info-card'>
                    <strong>📊 Grafana</strong><br>
                    <code style='background: #f39c12; color: white; padding: 5px; border-radius: 3px;'>http://localhost:3000</code>
                </div>
                <div class='info-card'>
                    <strong>🗄️ PostgreSQL</strong><br>
                    <code style='background: #9b59b6; color: white; padding: 5px; border-radius: 3px;'>192.168.182.3:5432</code>
                </div>
            </div>
        </div>
        
        <div class='section'>
            <h3><span class='emoji'>🌟</span>Sistem Özellikleri:</h3>
            <ul style='list-style: none; padding: 0;'>
                <li><span class='emoji'>✅</span>Nakit Akış Analizi API'leri</li>
                <li><span class='emoji'>✅</span>Grafana Dashboard Entegrasyonu</li>
                <li><span class='emoji'>✅</span>React Frontend Desteği</li>
                <li><span class='emoji'>✅</span>Export (Excel/PDF) İşlemleri</li>
                <li><span class='emoji'>✅</span>Hangfire Background Jobs</li>
                <li><span class='emoji'>✅</span>CORS Desteği</li>
                <li><span class='emoji'>✅</span>Serilog Logging</li>
                <li><span class='emoji'>✅</span>Health Checks</li>
            </ul>
        </div>
        
        <div style='text-align: center; margin-top: 30px; padding: 20px; background: #34495e; color: white; border-radius: 6px;'>
            <p><strong>🎯 NakitAkış Dashboard Suite v1.0</strong></p>
            <p>React + .NET API + PostgreSQL + Grafana</p>
            <small>Geliştirici: Gökhan Yaman | {DateTime.Now:yyyy}</small>
        </div>
    </div>
</body>
</html>", "text/html; charset=utf-8"));


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