using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NakitAkis.Models;
using NakitAkis.Services;

namespace NakitAkis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrafanaController : ControllerBase
    {
        private readonly INakitAkisService _nakitAkisService;
        private readonly ILogger<GrafanaController> _logger;

        public GrafanaController(INakitAkisService nakitAkisService, ILogger<GrafanaController> logger)
        {
            _nakitAkisService = nakitAkisService;
            _logger = logger;
        }

        // Grafana Health Check
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        // Grafana Test Connection
        [HttpPost("test")]
        public async Task<IActionResult> TestDataSource()
        {
            try
            {
                var isConnected = await _nakitAkisService.TestConnectionAsync();
                return Ok(new { status = isConnected ? "success" : "error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grafana test connection failed");
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        // Grafana Query Endpoint - Ana veri kaynağı
        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] object requestData)
        {
            try
            {
                _logger.LogInformation("Grafana query received: {request}", requestData);

                // Default parametreler
                var parametreler = new NakitAkisParametre
                {
                    FaizOrani = 0.45m,
                    KaynakKurulus = "FİBABANKA"
                };

                var sonuc = await _nakitAkisService.GetNakitAkisAnaliziAsync(parametreler);

                // Grafana için basit format - sadece değerler
                var result = new[]
                {
                    new
                    {
                        target = "nakit_akis.toplam_faiz",
                        datapoints = new object[][]
                        {
                            new object[] { (double)sonuc.ToplamFaizTutari, 1736241075000 }
                        }
                    },
                    new
                    {
                        target = "nakit_akis.model_faiz",
                        datapoints = new object[][]
                        {
                            new object[] { (double)sonuc.ToplamModelFaizTutari, 1736241075000 }
                        }
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grafana query failed: {error}", ex.Message);
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
        }
    
        
    

        // Grafana Search - Metrik keşfi
        [HttpPost("search")]
        public IActionResult Search([FromBody] GrafanaSearchRequest request)
        {
            try
            {
                var metrics = new[]
                {
                    "nakit_akis.toplam_faiz",
                    "nakit_akis.model_faiz",
                    "nakit_akis.fark_tutari",
                    "nakit_akis.fark_yuzdesi",
                    "nakit_akis.kaynak_kurulus_bazli",
                    "nakit_akis.banka_bazli"
                };

                var filteredMetrics = string.IsNullOrEmpty(request.Target)
                    ? metrics
                    : metrics.Where(m => m.Contains(request.Target, StringComparison.OrdinalIgnoreCase)).ToArray();

                return Ok(filteredMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grafana search failed");
                return BadRequest(new { error = ex.Message });
            }
        }

        // Grafana Variable Support
        [HttpPost("variable")]
        public async Task<IActionResult> Variable([FromBody] GrafanaVariableRequest request)
        {
            try
            {
                switch (request.Variable?.ToLower())
                {
                    case "kaynak_kurulus":
                        var kuruluslar = await _nakitAkisService.GetKaynakKuruluslarAsync();
                        return Ok(kuruluslar.Select(k => new { text = k.KaynakKurulus, value = k.KaynakKurulus }));

                    case "bankalar":
                        var bankalar = await _nakitAkisService.GetBankalarAsync();
                        return Ok(bankalar.Select(b => new { text = b.BankaAdi, value = b.BankaAdi }));

                    default:
                        return Ok(new[] { new { text = "Değişken bulunamadı", value = "" } });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grafana variable query failed");
                return BadRequest(new { error = ex.Message });
            }
        }

        // Grafana Annotations
        [HttpPost("annotations")]
        public IActionResult Annotations([FromBody] GrafanaAnnotationRequest request)
        {
            try
            {
                // Önemli olayları annotation olarak döndür
                var annotations = new[]
                {
                    new GrafanaAnnotation
                    {
                        Time = DateTime.UtcNow.AddDays(-30).Ticks,
                        Title = "Sistem Güncellemesi",
                        Text = "Nakit akış hesaplama motoru güncellendi",
                        Tags = new[] { "sistem", "güncelleme" }
                    },
                    new GrafanaAnnotation
                    {
                        Time = DateTime.UtcNow.AddDays(-7).Ticks,
                        Title = "Faiz Oranı Değişikliği",
                        Text = "Merkez Bankası faiz oranını %45'e çıkardı",
                        Tags = new[] { "faiz", "merkez-bankası" }
                    }
                };

                return Ok(annotations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grafana annotations failed");
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<GrafanaQueryResult> ProcessTarget(GrafanaTarget target, GrafanaRange range)
        {
            // Default parametreler
            var parametreler = new NakitAkisParametre
            {
                FaizOrani = 0.45m,
                KaynakKurulus = "FİBABANKA"
            };

            // Target'dan parametreleri parse et
            if (target.Params != null)
            {
                if (target.Params.ContainsKey("faizOrani") && decimal.TryParse(target.Params["faizOrani"], out var faizOrani))
                    parametreler.FaizOrani = faizOrani;

                if (target.Params.ContainsKey("kaynakKurulus"))
                    parametreler.KaynakKurulus = target.Params["kaynakKurulus"];

                if (target.Params.ContainsKey("bankalar"))
                    parametreler.SecilenBankalar = target.Params["bankalar"].Split(',').ToList();
            }

            var sonuc = await _nakitAkisService.GetNakitAkisAnaliziAsync(parametreler);

            // Metrik tipine göre veri döndür
            var dataPoints = new List<decimal[]>();
            var currentTime = DateTime.UtcNow.Ticks;

            switch (target.Target?.ToLower())
            {
                case "nakit_akis.toplam_faiz":
                    dataPoints.Add(new[] { sonuc.ToplamFaizTutari, currentTime });
                    break;

                case "nakit_akis.model_faiz":
                    dataPoints.Add(new[] { sonuc.ToplamModelFaizTutari, currentTime });
                    break;

                case "nakit_akis.fark_tutari":
                    dataPoints.Add(new[] { sonuc.FarkTutari, currentTime });
                    break;

                case "nakit_akis.fark_yuzdesi":
                    dataPoints.Add(new[] { sonuc.FarkYuzdesi, currentTime });
                    break;

                default:
                    // Tüm metrikleri döndür
                    dataPoints.AddRange(new[]
                    {
                        new[] { sonuc.ToplamFaizTutari, currentTime },
                        new[] { sonuc.ToplamModelFaizTutari, currentTime },
                        new[] { sonuc.FarkTutari, currentTime }
                    });
                    break;
            }

            return new GrafanaQueryResult
            {
                Target = target.Target ?? "nakit_akis",
                DataPoints = dataPoints
            };
        }
    }
}
