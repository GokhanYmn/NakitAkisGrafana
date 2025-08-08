using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NakitAkis.Models;
using NakitAkis.Services;
using Nest;
using Npgsql;
using System.Text.Json;

namespace NakitAkis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrafanaController : ControllerBase
    {
        private readonly INakitAkisService _nakitAkisService;
        private readonly ILogger<GrafanaController> _logger;
        private readonly IConfiguration _configuration;
        public GrafanaController(INakitAkisService nakitAkisService, ILogger<GrafanaController> logger, IConfiguration configuration)
        {
            _nakitAkisService = nakitAkisService;
            _logger = logger;
            _configuration = configuration;
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
        [HttpGet("query")]
        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] object requestData = null,
                                      [FromQuery] string kaynak_kurulus = null,
                                      [FromQuery] string fm_fonlar = null)
        {
            try
            {
                _logger.LogInformation("=== QUERY REQUEST ===");
                _logger.LogInformation("Method: {method}", HttpContext.Request.Method);
                _logger.LogInformation("Query kaynak_kurulus: {kurulus}", kaynak_kurulus);
                _logger.LogInformation("Query fm_fonlar: {fonlar}", fm_fonlar);

                if (requestData != null)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(requestData);
                    _logger.LogInformation("Full request: {request}", jsonString);
                }

                // Default parametreler
                var parametreler = new NakitAkisParametre
                {
                    FaizOrani = 0.45m,
                    ModelFaizOrani = 0.45m,
                    KaynakKurulus = kaynak_kurulus ?? "FİBABANKA",  // YENİ - GET parametresini kullan
                    SecilenFonNo = string.IsNullOrEmpty(fm_fonlar) || fm_fonlar == "$__all" ? null : fm_fonlar,  // YENİ
                    SecilenIhracNo = null
                };

                // JSON'dan variable'ları parse etmeye çalış (sadece POST için)
                if (requestData != null)
                {
                    try
                    {
                        var jsonString = System.Text.Json.JsonSerializer.Serialize(requestData);
                        var jsonElement = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonString);

                        // scopedVars'dan variable'ları çek
                        if (jsonElement.TryGetProperty("scopedVars", out var scopedVars))
                        {
                            if (scopedVars.TryGetProperty("kaynak_kurulus", out var kaynakVar) &&
                                kaynakVar.TryGetProperty("value", out var kaynakValue))
                            {
                                parametreler.KaynakKurulus = kaynakValue.GetString() ?? "FİBABANKA";
                                _logger.LogInformation("Kaynak kurulus from POST variable: {kurulus}", parametreler.KaynakKurulus);
                            }

                            if (scopedVars.TryGetProperty("fm_fonlar", out var fonVar) &&  // YENİ - fm_fonlar
                                fonVar.TryGetProperty("value", out var fonValue))
                            {
                                var fonStr = fonValue.GetString();
                                if (!string.IsNullOrEmpty(fonStr) && fonStr != "$__all")
                                {
                                    parametreler.SecilenFonNo = fonStr;
                                    _logger.LogInformation("Fon no from POST variable: {fon}", parametreler.SecilenFonNo);
                                }
                            }

                            if (scopedVars.TryGetProperty("ihrac_no", out var ihracVar) &&
                                ihracVar.TryGetProperty("value", out var ihracValue))
                            {
                                var ihracStr = ihracValue.GetString();
                                if (!string.IsNullOrEmpty(ihracStr))
                                {
                                    parametreler.SecilenIhracNo = ihracStr;
                                    _logger.LogInformation("Ihrac no from POST variable: {ihrac}", parametreler.SecilenIhracNo);
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning("Variable parsing failed, using defaults: {error}", parseEx.Message);
                    }
                }

                _logger.LogInformation("Final parameters - Kurulus: {kurulus}, Fon: {fon}, Ihrac: {ihrac}",
                    parametreler.KaynakKurulus, parametreler.SecilenFonNo, parametreler.SecilenIhracNo);

                // Basit tarih aralığı
                DateTime fromDate = DateTime.UtcNow.AddMonths(-6);
                DateTime toDate = DateTime.UtcNow;

                var historicalData = await _nakitAkisService.GetHistoricalDataAsync(
                    parametreler, fromDate, toDate);

                _logger.LogInformation("Historical data count: {count} for kurulus: {kurulus}, fon: {fon}, ihrac: {ihrac}",
                    historicalData.Count, parametreler.KaynakKurulus, parametreler.SecilenFonNo, parametreler.SecilenIhracNo);

                // Grafana formatına çevir
                var toplamFaizDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.ToplamFaizTutari,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var modelFaizDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.ToplamModelFaizTutari,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var farkTutariDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.FarkTutari,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var farkYuzdesiDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.FarkYuzdesi,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var result = new[]
                {
            new
            {
                target = "nakit_akis.toplam_faiz",
                datapoints = toplamFaizDataPoints
            },
            new
            {
                target = "nakit_akis.model_faiz",
                datapoints = modelFaizDataPoints
            },
            new
            {
                target = "nakit_akis.fark_tutari",
                datapoints = farkTutariDataPoints
            },
            new
            {
                target = "nakit_akis.fark_yuzdesi",
                datapoints = farkYuzdesiDataPoints
            }
        };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grafana query failed: {error} - {details}", ex.Message, ex.ToString());
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
        }

        // Helper method ekleyin
        private TimeSpan ParseGrafanaTimeRange(string range)
        {
            // "now-6M", "now-30d", "now-24h" formatlarını parse et
            if (range.StartsWith("now-"))
            {
                var timeStr = range.Substring(4);
                if (timeStr.EndsWith("M"))
                {
                    if (int.TryParse(timeStr.TrimEnd('M'), out var months))
                        return TimeSpan.FromDays(-months * 30);
                }
                else if (timeStr.EndsWith("d"))
                {
                    if (int.TryParse(timeStr.TrimEnd('d'), out var days))
                        return TimeSpan.FromDays(-days);
                }
                else if (timeStr.EndsWith("h"))
                {
                    if (int.TryParse(timeStr.TrimEnd('h'), out var hours))
                        return TimeSpan.FromHours(-hours);
                }
            }
            return TimeSpan.FromDays(-180); // Default 6 ay
        }

        [HttpGet("trends")]
        [HttpPost("trends")]
        public async Task<IActionResult> GetTrends([FromQuery] string kaynak_kurulus = "FİBABANKA",
                                   [FromQuery] string fm_fonlar = null,
                                   [FromQuery] string period = "week")
        {
            try
            {
                _logger.LogInformation("=== TRENDS REQUEST ===");
                _logger.LogInformation("Kurulus: {kurulus}, Fon: {fon}, Period: {period}",
                    kaynak_kurulus, fm_fonlar, period);

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                var fonFilter = string.IsNullOrEmpty(fm_fonlar) || fm_fonlar == "All" || fm_fonlar == "$__all" ?
                    "" : " AND fon_no::text = @FonNo";

                // HAFTALIK KÜMÜLATİF BÜYÜME ANALİZİ
                var sql = $@"
            WITH haftalik_veriler AS (
                SELECT 
                    DATE_TRUNC('week', baslangic_tarihi) as hafta,
                    COALESCE(fon_no::text, 'bilinmiyor') as fon_no_str,
                    SUM(COALESCE(mevduat_tutari, 0)) as haftalik_mevduat,
                    SUM(COALESCE(faiz_tutari, 0)) as haftalik_faiz_kazanci,
                    COUNT(*) as haftalik_islem_sayisi,
                    AVG(COALESCE(faiz_orani, 0)) * 100 as ortalama_faiz_orani_yuzde
                FROM nakit_akis 
                WHERE kaynak_kurulus = @KaynakKurulus
                  AND baslangic_tarihi >= @FromDate
                  AND baslangic_tarihi <= @ToDate
                  AND baslangic_tarihi IS NOT NULL
                  AND COALESCE(mevduat_tutari, 0) > 0
                  {fonFilter}
                GROUP BY DATE_TRUNC('week', baslangic_tarihi), COALESCE(fon_no::text, 'bilinmiyor')
            ),
            kumulatif_hesaplamalar AS (
                SELECT 
                    hafta,
                    fon_no_str,
                    haftalik_mevduat,
                    haftalik_faiz_kazanci,
                    haftalik_islem_sayisi,
                    ortalama_faiz_orani_yuzde,
                    -- KÜMÜLATİF HESAPLAMALAR
                    SUM(haftalik_mevduat) OVER (
                        PARTITION BY fon_no_str 
                        ORDER BY hafta 
                        ROWS UNBOUNDED PRECEDING
                    ) as kumulatif_mevduat,
                    SUM(haftalik_faiz_kazanci) OVER (
                        PARTITION BY fon_no_str 
                        ORDER BY hafta 
                        ROWS UNBOUNDED PRECEDING
                    ) as kumulatif_faiz_kazanci,
                    -- BÜYÜME ORANI HESAPLAMALARI
                    CASE 
                        WHEN LAG(haftalik_mevduat) OVER (PARTITION BY fon_no_str ORDER BY hafta) > 0 THEN
                            ((haftalik_mevduat - LAG(haftalik_mevduat) OVER (PARTITION BY fon_no_str ORDER BY hafta)) / 
                             LAG(haftalik_mevduat) OVER (PARTITION BY fon_no_str ORDER BY hafta)) * 100
                        ELSE 0
                    END as haftalik_buyume_yuzde,
                    -- KÜMÜLATİF BÜYÜME ORANI (İlk haftaya göre)
                    CASE 
                        WHEN FIRST_VALUE(haftalik_mevduat) OVER (PARTITION BY fon_no_str ORDER BY hafta ROWS UNBOUNDED PRECEDING) > 0 THEN
                            ((haftalik_mevduat - FIRST_VALUE(haftalik_mevduat) OVER (PARTITION BY fon_no_str ORDER BY hafta ROWS UNBOUNDED PRECEDING)) / 
                             FIRST_VALUE(haftalik_mevduat) OVER (PARTITION BY fon_no_str ORDER BY hafta ROWS UNBOUNDED PRECEDING)) * 100
                        ELSE 0
                    END as kumulatif_buyume_yuzde
                FROM haftalik_veriler
            )
            SELECT 
                hafta,
                fon_no_str,
                haftalik_mevduat,
                haftalik_faiz_kazanci,
                kumulatif_mevduat,
                kumulatif_faiz_kazanci,
                haftalik_buyume_yuzde,
                kumulatif_buyume_yuzde,
                haftalik_islem_sayisi,
                ortalama_faiz_orani_yuzde
            FROM kumulatif_hesaplamalar
            WHERE hafta IS NOT NULL
            ORDER BY hafta, fon_no_str";

                var result = await connection.QueryAsync<dynamic>(sql, new
                {
                    KaynakKurulus = kaynak_kurulus,
                    FonNo = fm_fonlar,
                    FromDate = DateTime.UtcNow.AddMonths(-6), // 6 aylık veri
                    ToDate = DateTime.UtcNow
                });

                if (!result.Any())
                {
                    _logger.LogWarning("No data found for kurulus: {kurulus}", kaynak_kurulus);
                    return Ok(new[] { new {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                mesaj = "Veri bulunamadı",
                kurulus = kaynak_kurulus
            }});
                }

                // GRAFANA İÇİN BASIT FORMAT
                var response = result.Select(r => new
                {
                    timestamp = ((DateTimeOffset)((DateTime)r.hafta)).ToUnixTimeMilliseconds(),
                    hafta = ((DateTime)r.hafta).ToString("yyyy-MM-dd"),
                    fon_no = r.fon_no_str?.ToString() ?? "bilinmiyor",

                    // MEVDUAT VERİLERİ
                    haftalik_mevduat = Convert.ToDouble(r.haftalik_mevduat ?? 0),
                    kumulatif_mevduat = Convert.ToDouble(r.kumulatif_mevduat ?? 0),

                    // FAİZ KAZANCI VERİLERİ
                    haftalik_faiz_kazanci = Convert.ToDouble(r.haftalik_faiz_kazanci ?? 0),
                    kumulatif_faiz_kazanci = Convert.ToDouble(r.kumulatif_faiz_kazanci ?? 0),

                    // BÜYÜME VERİLERİ (ÖNEMLİ!)
                    haftalik_buyume_yuzde = Math.Round(Convert.ToDouble(r.haftalik_buyume_yuzde ?? 0), 2),
                    kumulatif_buyume_yuzde = Math.Round(Convert.ToDouble(r.kumulatif_buyume_yuzde ?? 0), 2),

                    // EK BİLGİLER
                    haftalik_islem_sayisi = Convert.ToInt32(r.haftalik_islem_sayisi ?? 0),
                    ortalama_faiz_orani = Math.Round(Convert.ToDouble(r.ortalama_faiz_orani_yuzde ?? 0), 2),

                    kurulus = kaynak_kurulus
                }).ToArray();

                _logger.LogInformation("Trends returning {count} weekly records with cumulative growth", response.Length);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trends query failed: {error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
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
        // Variable destekli query endpoint
        [HttpGet("query-with-variable")]
        public async Task<IActionResult> QueryWithVariable([FromQuery] string kaynak_kurulus = "FİBABANKA")
        {
            try
            {
                _logger.LogInformation("=== VARIABLE DEBUG ===");
                _logger.LogInformation("Query parameter kaynak_kurulus: '{kurulus}'", kaynak_kurulus);
                _logger.LogInformation("Parameter is null: {isNull}", kaynak_kurulus == null);
                _logger.LogInformation("Parameter is empty: {isEmpty}", string.IsNullOrEmpty(kaynak_kurulus));
                _logger.LogInformation("========================");

                var parametreler = new NakitAkisParametre
                {
                    FaizOrani = 0.45m,
                    KaynakKurulus = kaynak_kurulus ?? "FİBABANKA"
                };

                _logger.LogInformation("Final parameter kaynak_kurulus: '{kurulus}'", parametreler.KaynakKurulus);

                DateTime fromDate = DateTime.UtcNow.AddYears(-2);
                DateTime toDate = DateTime.UtcNow;

                var historicalData = await _nakitAkisService.GetHistoricalDataAsync(
                    parametreler, fromDate, toDate);

                _logger.LogInformation("Historical data count: {count} for kurulus: {kurulus}", historicalData.Count, kaynak_kurulus);

                var toplamFaizDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.ToplamFaizTutari,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var modelFaizDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.ToplamModelFaizTutari,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var farkTutariDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.FarkTutari,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var farkYuzdesiDataPoints = historicalData.Select(h => new object[]
                {
            (double)h.FarkYuzdesi,
            ((DateTimeOffset)h.Tarih.Date).ToUnixTimeMilliseconds()
                }).ToArray();

                var result = new[]
                {
            new
            {
                target = "nakit_akis.toplam_faiz",
                datapoints = toplamFaizDataPoints
            },
            new
            {
                target = "nakit_akis.model_faiz",
                datapoints = modelFaizDataPoints
            },
            new
            {
                target = "nakit_akis.fark_tutari",
                datapoints = farkTutariDataPoints
            },
            new
            {
                target = "nakit_akis.fark_yuzdesi",
                datapoints = farkYuzdesiDataPoints
            }
        };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Variable query failed: {error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // Grafana Variable Support
        [HttpGet("variable")]
        [HttpPost("variable")]
        public async Task<IActionResult> Variable([FromBody] GrafanaVariableRequest request = null,
                                         [FromQuery] string variable = null,
                                         [FromQuery] string kaynak_kurulus = null,
                                         [FromQuery] string fm_fonlar = null,
                                         [FromQuery] string ihrac_no = null
            )
        {
            try
            {
                _logger.LogInformation("=== VARIABLE REQUEST ===");
                _logger.LogInformation("Variable: {variable}", variable);
                _logger.LogInformation("Kaynak Kurulus: {kurulus}", kaynak_kurulus);
                _logger.LogInformation("FM Fonlar: {fonlar}", fm_fonlar);
                _logger.LogInformation("Ihrac No: {ihrac}", ihrac_no);
                _logger.LogInformation("Request method: {method}", HttpContext.Request.Method);

                var variableName = request?.Variable ?? variable;

                if (string.IsNullOrEmpty(variableName))
                {
                    _logger.LogWarning("Variable name is empty");
                    return BadRequest(new { error = "Variable parameter is required" });
                }

                switch (variableName.ToLower())
                {
                    case "kaynak_kurulus":
                        _logger.LogInformation("Getting kaynak kurulus list");
                        var kuruluslar = await _nakitAkisService.GetKaynakKuruluslarAsync();
                        _logger.LogInformation("Found {count} kurulus", kuruluslar.Count);
                        return Ok(kuruluslar.Select(k => new { text = k.KaynakKurulus, value = k.KaynakKurulus }));

                    case "fon_no":
                    case "fm_fonlar":
                        var kurulus = kaynak_kurulus ?? "FİBABANKA";
                        _logger.LogInformation("Getting fonlar for kurulus: {kurulus}", kurulus);

                        var fonlar = await _nakitAkisService.GetFonlarAsync(kurulus);
                        _logger.LogInformation("Found {count} fonlar", fonlar.Count);

                        if (fonlar.Any())
                        {
                            var result = fonlar.Select(f => new
                            {
                                text = $"{f.FonNo} (₺{f.ToplamTutar:N0})",
                                value = f.FonNo
                            }).ToList();

                            _logger.LogInformation("Returning {count} fon options", result.Count);
                            return Ok(result);
                        }
                        else
                        {
                            _logger.LogWarning("No fonlar found for kurulus: {kurulus}", kurulus);
                            return Ok(new[] { new { text = "Fon bulunamadı", value = "" } });
                        }
                    case "ihrac_no":
                        var kurulusIhrac = kaynak_kurulus ?? "FİBABANKA";
                        var fonNoIhrac = fm_fonlar;

                        _logger.LogInformation("Getting ihraclar for kurulus: '{kurulus}', fon: '{fon}'", kurulusIhrac, fonNoIhrac);

                        try
                        {
                            var ihraclar = await _nakitAkisService.GetIhraclarAsync(kurulusIhrac, fonNoIhrac);
                            _logger.LogInformation("Found {count} ihraclar for fon: {fon}", ihraclar.Count, fonNoIhrac);

                            if (ihraclar.Any())
                            {
                                var ihracResult = ihraclar.Select(i => new
                                {
                                    text = $"{i.IhracNo} (₺{i.ToplamTutar:N0})",
                                    value = i.IhracNo
                                }).ToList();

                                return Ok(ihracResult);
                            }
                            else
                            {
                                return Ok(new[] { new { text = "İhraç bulunamadı", value = "" } });
                            }
                        }
                        catch (Exception ihracEx)
                        {
                            _logger.LogError(ihracEx, "Error getting ihraclar");
                            return Ok(new[] { new { text = "İhraç yüklenirken hata", value = "" } });
                        }

                    default:
                        _logger.LogWarning("Unknown variable: {variable}", variableName);
                        return BadRequest(new { error = $"Unknown variable: {variableName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Variable query failed");
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
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
