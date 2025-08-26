using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NakitAkis.Models;
using NakitAkis.Services;
using Nest;
using Npgsql;
using System.Text;
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

        [HttpGet("cash-flow-analysis")]
        [HttpPost("cash-flow-analysis")]
        public async Task<IActionResult> GetCashFlowAnalysis([FromQuery] string period = "month",
                                                    [FromQuery] int limit = 100)
        {
            try
            {
                _logger.LogInformation("=== CASH FLOW ANALYSIS REQUEST ===");
                _logger.LogInformation("Period: {period}, Limit: {limit}", period, limit);

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Period'a göre DATE_TRUNC belirle
                string dateTrunc = period switch
                {
                    "day" => "day",
                    "week" => "week",
                    "month" => "month",
                    "quarter" => "quarter",
                    "year" => "year",
                    _ => "month"
                };

                // TEK QUERY - SADECE DATE_TRUNC İLE GRUPLAMA
                var sql = $@"
        SELECT 
            DATE_TRUNC('{dateTrunc}', tarih) as period_date,
            -- VERİLERİ OLDUĞU GİBİ AL, FAZLA TOPLAMA YAPMA
            AVG(COALESCE(anapara, 0))::float8 as total_anapara,
            AVG(COALESCE(basit_faiz, 0))::float8 as total_basit_faiz,
            AVG(COALESCE(faiz_kznc, 0))::float8 as total_faiz_kazanci,
            AVG(COALESCE(model_faiz_kznc, 0))::float8 as total_model_faiz_kazanci,
            AVG(COALESCE(tlref_faiz_kazanci, 0))::float8 as total_tlref_kazanci,
            COUNT(*) as record_count,
            AVG(COALESCE(model_nema_orani, 0))::float8 as avg_model_nema_orani,
            AVG(COALESCE(tlref_faiz, 0))::float8 as avg_tlref_faiz,
            AVG(COALESCE(basit_faiz, 0))::float8 as avg_basit_faiz,
            0.0 as total_model_faiz,
            0.0 as total_tlref_faiz
        FROM cash_flow_analysis 
        WHERE tarih IS NOT NULL 
          AND anapara > 0
        GROUP BY DATE_TRUNC('{dateTrunc}', tarih)
        ORDER BY DATE_TRUNC('{dateTrunc}', tarih) ASC
        LIMIT @Limit";

                var result = await connection.QueryAsync<dynamic>(sql, new { Limit = limit });

                if (!result.Any())
                {
                    _logger.LogWarning("No cash flow analysis data found");
                    return Ok(new[] { new {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                mesaj = "Cash flow analiz verisi bulunamadı"
            }});
                }

                var response = result.Select(r => new
                {
                    timestamp = ((DateTimeOffset)((DateTime)r.period_date)).ToUnixTimeMilliseconds(),
                    period = ((DateTime)r.period_date).ToString("yyyy-MM-dd"),

                    // SADECE CONVERT ET, HESAPLAMA YAPMA
                    total_anapara = SafeConvertToDouble(r.total_anapara),
                    total_basit_faiz = SafeConvertToDouble(r.total_basit_faiz),
                    total_faiz_kazanci = SafeConvertToDouble(r.total_faiz_kazanci),
                    avg_basit_faiz = SafeConvertToDouble(r.avg_basit_faiz),
                    total_model_faiz = SafeConvertToDouble(r.total_model_faiz),
                    total_model_faiz_kazanci = SafeConvertToDouble(r.total_model_faiz_kazanci),
                    avg_model_nema_orani = SafeConvertToDouble(r.avg_model_nema_orani),
                    total_tlref_faiz = SafeConvertToDouble(r.total_tlref_faiz),
                    total_tlref_kazanci = SafeConvertToDouble(r.total_tlref_kazanci),
                    avg_tlref_faiz = SafeConvertToDouble(r.avg_tlref_faiz),

                    // VERİMLİLİK HESAPLAMALARI - FRONTEND'DE YAPILACAK
                    basit_faiz_yield_percentage = SafeConvertToDouble(r.total_anapara) > 0
                        ? (SafeConvertToDouble(r.total_faiz_kazanci) / SafeConvertToDouble(r.total_anapara) * 100)
                        : 0.0,
                    model_faiz_yield_percentage = SafeConvertToDouble(r.total_anapara) > 0
                        ? (SafeConvertToDouble(r.total_model_faiz_kazanci) / SafeConvertToDouble(r.total_anapara) * 100)
                        : 0.0,
                    tlref_faiz_yield_percentage = SafeConvertToDouble(r.total_anapara) > 0
                        ? (SafeConvertToDouble(r.total_tlref_kazanci) / SafeConvertToDouble(r.total_anapara) * 100)
                        : 0.0,

                    // PERFORMANS HESAPLAMALARI
                    basit_vs_model_performance = SafeConvertToDouble(r.total_model_faiz_kazanci) > 0
                        ? ((SafeConvertToDouble(r.total_faiz_kazanci) - SafeConvertToDouble(r.total_model_faiz_kazanci)) / SafeConvertToDouble(r.total_model_faiz_kazanci) * 100)
                        : 0.0,
                    basit_vs_tlref_performance = SafeConvertToDouble(r.total_tlref_kazanci) > 0
                        ? ((SafeConvertToDouble(r.total_faiz_kazanci) - SafeConvertToDouble(r.total_tlref_kazanci)) / SafeConvertToDouble(r.total_tlref_kazanci) * 100)
                        : 0.0,

                    record_count = Convert.ToInt32(r.record_count ?? 0),
                    period_type = period
                }).ToArray();

                _logger.LogInformation("Cash flow analysis returning {count} {period} records",
                    response.Length, period);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cash flow analysis query failed: {error}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper method ekle
        private double SafeConvertToDouble(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0.0;

            try
            {
                return Convert.ToDouble(value);
            }
            catch (OverflowException)
            {
                _logger.LogWarning("Overflow detected, returning max safe value");
                return double.MaxValue / 1000; // Güvenli değer
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Value conversion failed, returning 0");
                return 0.0;
            }
        }

       
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
                    KaynakKurulus = kaynak_kurulus ?? "FİBABANKA",  
                    SecilenFonNo = string.IsNullOrEmpty(fm_fonlar) || fm_fonlar == "$__all" ? null : fm_fonlar, 
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

        // Helper method
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
                                   [FromQuery] string ihrac_no = null,
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
                var ihracFilter = string.IsNullOrEmpty(ihrac_no) || ihrac_no == "All" || ihrac_no == "$__all" ?
                    "" : " AND ihrac_no::text = @IhracNo";
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
                  {ihracFilter}
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
                    IhracNo = ihrac_no,
                    FromDate = DateTime.Parse("2020-01-01"),
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
        [HttpGet("analysis")]
        [HttpPost("analysis")]
        public async Task<IActionResult> GetAnalysis([FromQuery] decimal faizOrani = 45,
                                           [FromQuery] string kaynak_kurulus = "ARZUM",
                                           [FromQuery] string fm_fonlar = null,
                                           [FromQuery] string ihrac_no = null)
        {
            try
            {
                Console.WriteLine($"Analysis with filters: {faizOrani}%, {kaynak_kurulus}, Fon: {fm_fonlar}, Ihrac: {ihrac_no}");

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // FİLTRE LOGIC'İ
                var fonFilter = "";
                var ihracFilter = "";

                if (!string.IsNullOrEmpty(fm_fonlar) && fm_fonlar != "All" && fm_fonlar != "$__all")
                {
                    fonFilter = " AND fon_no::text = @FonNo";
                }

                if (!string.IsNullOrEmpty(ihrac_no) && ihrac_no != "All" && ihrac_no != "$__all")
                {
                    ihracFilter = " AND ihrac_no::text = @IhracNo";
                }

                // DYNAMIC SQL WITH FILTERS
                var sql = $@"
            SELECT 
                SUM(COALESCE(mevduat_tutari, 0)) as toplam_mevduat,
                SUM(COALESCE(faiz_tutari, 0)) as gercek_faiz_tutari,
                COUNT(*) as toplam_islem,
                AVG(COALESCE(donus_tarihi - baslangic_tarihi, 30)) as ortalama_vade
            FROM nakit_akis 
            WHERE kaynak_kurulus = @KaynakKurulus
              AND COALESCE(mevduat_tutari, 0) > 0
              AND donus_tarihi > baslangic_tarihi
              {fonFilter}
              {ihracFilter}";

                Console.WriteLine($"SQL: {sql}");

                var result = await connection.QueryFirstOrDefaultAsync(sql, new
                {
                    KaynakKurulus = kaynak_kurulus,
                    FonNo = fm_fonlar,
                    IhracNo = ihrac_no
                });

                if (result == null)
                {
                    Console.WriteLine("No filtered data found");
                    return Ok(new
                    {
                        toplamFaizTutari = 0,
                        toplamModelFaizTutari = 0,
                        farkTutari = 0,
                        farkYuzdesi = 0,
                        faizOrani = faizOrani,
                        mesaj = "Filtrelenmiş veri bulunamadı"
                    });
                }

                // GRAFANA STYLE HESAPLAMA
                decimal gercekFaiz = Convert.ToDecimal(result.gercek_faiz_tutari ?? 0);
                decimal toplamMevduat = Convert.ToDecimal(result.toplam_mevduat ?? 0);
                double ortalamaVade = Convert.ToDouble(result.ortalama_vade ?? 30);

                // Model faiz = Mevduat * Faiz * (Vade/365)
                decimal modelFaiz = toplamMevduat * (faizOrani / 100m) * ((decimal)ortalamaVade / 365m);

                decimal fark = gercekFaiz - modelFaiz;
                double farkYuzde = modelFaiz > 0 ? (double)(fark / modelFaiz * 100) : 0;

                var response = new
                {
                    toplamFaizTutari = gercekFaiz,
                    toplamModelFaizTutari = modelFaiz,
                    farkTutari = fark,
                    farkYuzdesi = Math.Round(farkYuzde, 2),
                    faizOrani = faizOrani,
                    toplamMevduat = toplamMevduat,
                    toplamIslem = Convert.ToInt32(result.toplam_islem ?? 0),
                    ortalamaVade = Math.Round(ortalamaVade, 1),
                    mesaj = "Filtrelenmiş analiz başarılı"
                };

                Console.WriteLine($"Filtered Analysis: Gerçek={gercekFaiz}, Model={modelFaiz}, Vade={ortalamaVade} gün");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Filtered analysis error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAnalysis([FromQuery] string level = "basic",
                                               [FromQuery] string format = "excel",
                                               [FromQuery] decimal faizOrani = 45,
                                               [FromQuery] string kaynak_kurulus = "ARZUM",
                                               [FromQuery] string fm_fonlar = null,
                                               [FromQuery] string ihrac_no = null)
        {
            try
            {
                Console.WriteLine($"Export request: Level={level}, Format={format}, {faizOrani}%, {kaynak_kurulus}");

                // Analysis verilerini al
                var analysisData = await GetAnalysisData(faizOrani, kaynak_kurulus, fm_fonlar, ihrac_no);
                var trendsData = await GetTrendsDataForExport(kaynak_kurulus, fm_fonlar, ihrac_no);

                if (analysisData == null)
                {
                    return BadRequest("Export için veri bulunamadı");
                }

                // Level'a göre içerik oluştur
                switch (format.ToLower())
                {
                    case "excel":
                        return await GenerateExcelExport(level, analysisData, trendsData, faizOrani, kaynak_kurulus, fm_fonlar, ihrac_no);
                    case "pdf":
                        return await GeneratePDFExport(level, analysisData, trendsData, faizOrani, kaynak_kurulus, fm_fonlar, ihrac_no);
                    default:
                        return BadRequest("Geçersiz format. 'excel' veya 'pdf' kullanın.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Excel Export Method
        private async Task<IActionResult> GenerateExcelExport(string level, dynamic analysisData, List<dynamic> trendsData,
                                                             decimal faizOrani, string kurulus, string fon, string ihrac)
        {
            var csvContent = new StringBuilder();

            // Header
            csvContent.AppendLine("Nakit Akış Analizi Raporu");
            csvContent.AppendLine($"Export Level,{GetLevelName(level)}");
            csvContent.AppendLine($"Rapor Tarihi,{DateTime.Now:dd.MM.yyyy HH:mm}");
            csvContent.AppendLine($"Kuruluş,{kurulus}");
            csvContent.AppendLine($"Fon,{fon ?? "Tüm Fonlar"}");
            csvContent.AppendLine($"İhraç,{ihrac ?? "Tüm İhraçlar"}");
            csvContent.AppendLine($"Model Faiz Oranı,%{faizOrani}"); // DOĞRU FAİZ ORANI
            csvContent.AppendLine("");

            // Calculate values - FAİZ ORANI DÜZELTİLDİ
            decimal gercekFaiz = Convert.ToDecimal(analysisData.gercek_faiz_tutari ?? 0);
            decimal toplamMevduat = Convert.ToDecimal(analysisData.toplam_mevduat ?? 0);
            double ortalamaVade = Convert.ToDouble(analysisData.ortalama_vade ?? 30);
            decimal modelFaiz = toplamMevduat * (faizOrani / 100m) * ((decimal)ortalamaVade / 365m); // FAİZ ORANI PARAMETRESİ KULLANILIYOR
            decimal fark = gercekFaiz - modelFaiz;
            double farkYuzde = modelFaiz > 0 ? (double)(fark / modelFaiz * 100) : 0;

            // Level 1: Basic - ORTALAMA VADE KALDIRILDI
            csvContent.AppendLine("=== TEMEL ANALİZ SONUÇLARI ===");
            csvContent.AppendLine("Metrik,Değer");
            csvContent.AppendLine($"Toplam Mevduat,₺{toplamMevduat:N2}");
            csvContent.AppendLine($"Gerçek Faiz Tutarı,₺{gercekFaiz:N2}");
            csvContent.AppendLine($"Model Faiz Tutarı,₺{modelFaiz:N2}");
            csvContent.AppendLine($"Fark Tutarı,₺{fark:N2}");
            csvContent.AppendLine($"Fark Yüzdesi,%{farkYuzde:N2}");
            csvContent.AppendLine($"Toplam İşlem,{Convert.ToInt32(analysisData.toplam_islem ?? 0)}");

            if (level == "detailed" || level == "full")
            {
                // Level 2: Detailed Statistics
                csvContent.AppendLine("");
                csvContent.AppendLine("=== DETAYLI İSTATİSTİKLER ===");
                csvContent.AppendLine("İstatistik,Değer");
                csvContent.AppendLine($"Faiz Verimliliği,%{((gercekFaiz / toplamMevduat) * 100):N2}");
                csvContent.AppendLine($"Model Performans Oranı,%{((gercekFaiz / modelFaiz) * 100):N2}");
                csvContent.AppendLine($"Günlük Ortalama Kazanç,₺{(gercekFaiz / (decimal)ortalamaVade):N2}");
                csvContent.AppendLine($"İşlem Başına Ortalama,₺{(toplamMevduat / Convert.ToDecimal(analysisData.toplam_islem ?? 1)):N2}");

                // Trends Data - YAKIN TARİHDEN UZAĞA SIRALI
                if (trendsData?.Count > 0)
                {
                    csvContent.AppendLine("");
                    csvContent.AppendLine("=== HAFTALIK TREND VERİLERİ (Son 10 Hafta) ===");
                    csvContent.AppendLine("Tarih,Kümülatif Mevduat,Kümülatif Faiz");

                    // YAKIN TARİHDEN UZAĞA SIRALA
                    var sortedTrends = trendsData.OrderByDescending(t => Convert.ToDateTime(t.timestamp)).Take(10);

                    foreach (var trend in sortedTrends)
                    {
                        csvContent.AppendLine($"{Convert.ToDateTime(trend.timestamp):dd.MM.yyyy},₺{Convert.ToDecimal(trend.kumulatif_mevduat):N2},₺{Convert.ToDecimal(trend.kumulatif_faiz_kazanci):N2}");
                    }
                }
            }

            if (level == "full")
            {
                // Level 3: Full Analysis - RİSK SEVİYESİ VE VADE YAPISI KALDIRILDI
                csvContent.AppendLine("");
                csvContent.AppendLine("=== PERFORMANS DEĞERLENDİRMESİ ===");
                csvContent.AppendLine("Kategori,Durum,Yorum");
                csvContent.AppendLine($"Faiz Performansı,{(farkYuzde >= 0 ? "POZITIF" : "NEGATIF")},{(farkYuzde >= 0 ? "Model üzerinde performans" : "Model altında performans")}");

                csvContent.AppendLine("");
                csvContent.AppendLine("=== AKSİYON ÖNERİLERİ ===");
                csvContent.AppendLine("Öncelik,Öneri");
                if (farkYuzde < -5)
                    csvContent.AppendLine("YÜKSEK,Faiz stratejisi gözden geçirilmeli");
                if (farkYuzde >= 0)
                    csvContent.AppendLine("DÜŞÜK,Mevcut stratejiye devam edilebilir");
                if (Math.Abs(farkYuzde) < 2)
                    csvContent.AppendLine("BİLGİ,Model ile uyumlu performans");
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            var fileName = $"nakit_akis_{level}_{kurulus}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        
        // PDF Export Method
        private async Task<IActionResult> GeneratePDFExport(string level, dynamic analysisData, List<dynamic> trendsData,
                                                           decimal faizOrani, string kurulus, string fon, string ihrac)
        {
            // Calculate values - FAİZ ORANI DÜZELTİLDİ
            decimal gercekFaiz = Convert.ToDecimal(analysisData.gercek_faiz_tutari ?? 0);
            decimal toplamMevduat = Convert.ToDecimal(analysisData.toplam_mevduat ?? 0);
            double ortalamaVade = Convert.ToDouble(analysisData.ortalama_vade ?? 30);
            decimal modelFaiz = toplamMevduat * (faizOrani / 100m) * ((decimal)ortalamaVade / 365m); // FAİZ ORANI PARAMETRESİ
            decimal fark = gercekFaiz - modelFaiz;
            double farkYuzde = modelFaiz > 0 ? (double)(fark / modelFaiz * 100) : 0;

            var html = GenerateHTMLReportByLevel(level, gercekFaiz, toplamMevduat, modelFaiz, fark, farkYuzde,
                                                ortalamaVade, analysisData, trendsData, faizOrani, kurulus, fon, ihrac);

            var bytes = Encoding.UTF8.GetBytes(html);
            var fileName = $"nakit_akis_{level}_{kurulus}_{DateTime.Now:yyyyMMdd_HHmmss}.html";

            return File(bytes, "text/html", fileName);
        }

        private string GenerateHTMLReportByLevel(string level, decimal gercekFaiz, decimal toplamMevduat,
                                        decimal modelFaiz, decimal fark, double farkYuzde, double ortalamaVade,
                                        dynamic analysisData, List<dynamic> trendsData, decimal faizOrani,
                                        string kurulus, string fon, string ihrac)
        {
            var levelName = GetLevelName(level);
            var reportTitle = $"💰 Nakit Akış Analizi - {levelName}";

            var html = $@"
                    <!DOCTYPE html>
                        <html>
                        <head>
                    <meta charset='utf-8'>
                    <title>{reportTitle}</title>
                     <style>
                body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }}
                .header {{ text-align: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 30px; }}
                 .section {{ margin: 30px 0; }}
                 .level-badge {{ background: #007bff; color: white; padding: 5px 10px; border-radius: 15px; font-size: 12px; }}
                 table {{ width: 100%; border-collapse: collapse; margin: 15px 0; }}
                 th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
                 th {{ background-color: #f8f9fa; font-weight: bold; }}
                 .positive {{ color: #28a745; font-weight: bold; }}
                 .negative {{ color: #dc3545; font-weight: bold; }}
                 .metric-card {{ background: #f8f9fa; padding: 15px; margin: 10px 0; border-left: 4px solid #007bff; }}
                 .chart-placeholder {{ background: #e9ecef; padding: 20px; text-align: center; border-radius: 5px; margin: 15px 0; }}
                     </style>
                    </head>
                    <body>
                 <div class='header'>
                  <h1>{reportTitle}</h1>
                  <span class='level-badge'>{levelName}</span>
                   <p>Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                     </div>
    
                  <div class='section'>
                         <h2>📊 Analiz Parametreleri</h2>
                        <table>
                           <tr><th>Kuruluş</th><td>{kurulus}</td></tr>
                              <tr><th>Fon</th><td>{fon ?? "Tüm Fonlar"}</td></tr>
                               <tr><th>İhraç</th><td>{ihrac ?? "Tüm İhraçlar"}</td></tr>
                               <tr><th>Model Faiz Oranı</th><td>%{faizOrani}</td></tr>
                                 <tr><th>Export Level</th><td>{levelName}</td></tr>
                         </table>
                      </div>
    
    <div class='section'>
        <h2>💰 Temel Analiz Sonuçları</h2>
        <div class='metric-card'>
            <strong>Toplam Mevduat:</strong> ₺{toplamMevduat:N2}
        </div>
        <div class='metric-card'>
            <strong>Gerçek Faiz Tutarı:</strong> ₺{gercekFaiz:N2}
        </div>
        <div class='metric-card'>
            <strong>Model Faiz Tutarı:</strong> ₺{modelFaiz:N2}
        </div>
        <div class='metric-card'>
            <strong>Fark:</strong> <span class='{(fark >= 0 ? "positive" : "negative")}'>₺{fark:N2} (%{farkYuzde:N2})</span>
        </div>     
    </div>";

            // Level 2: Detailed
            if (level == "detailed" || level == "full")
            {
                html += $@"
    <div class='section'>
        <h2>📈 Detaylı İstatistikler</h2>
        <table>
            <tr><th>İstatistik</th><th>Değer</th></tr>
            <tr><td>Faiz Verimliliği</td><td>%{((gercekFaiz / toplamMevduat) * 100):N2}</td></tr>
            <tr><td>Model Performans Oranı</td><td>%{((gercekFaiz / modelFaiz) * 100):N2}</td></tr>
            <tr><td>Günlük Ortalama Kazanç</td><td>₺{(gercekFaiz / (decimal)ortalamaVade):N2}</td></tr>
            <tr><td>İşlem Başına Ortalama</td><td>₺{(toplamMevduat / Convert.ToDecimal(analysisData.toplam_islem ?? 1)):N2}</td></tr>
        </table>";

                if (trendsData?.Count > 0)
                {
                    html += $@"
        <h3>📊 Haftalık Trend Verileri (Son 5 Hafta)</h3>
        <table>
            <tr><th>Tarih</th><th>Kümülatif Mevduat</th><th>Kümülatif Faiz</th></tr>";

                    foreach (var trend in trendsData.Take(5))
                    {
                        html += $@"<tr>
                    <td>{Convert.ToDateTime(trend.timestamp):dd.MM.yyyy}</td>
                    <td>₺{Convert.ToDecimal(trend.kumulatif_mevduat):N2}</td>
                    <td>₺{Convert.ToDecimal(trend.kumulatif_faiz_kazanci):N2}</td>
                </tr>";
                    }
                    html += "</table>";
                }
                html += "</div>";
            }

            // Level 3: Full Report
            if (level == "full")
            {
                html += $@"
<div class='section'>
    <h2>🎯 Performans Değerlendirmesi</h2>
    <div class='metric-card'>
        <strong>Faiz Performansı:</strong> <span class='{(farkYuzde >= 0 ? "positive" : "negative")}'>{(farkYuzde >= 0 ? "POZİTİF" : "NEGATİF")}</span>
        <br><strong>Açıklama:</strong> {(farkYuzde >= 0 ? "Model üzerinde performans" : "Model altında performans")}
    </div>
</div>

<div class='section'>
    <h2>🔍 Aksiyon Önerileri</h2>
    <div class='metric-card'>";

                if (farkYuzde < -5)
                    html += "<strong>🚨 YÜKSEK ÖNCELİK:</strong> Faiz stratejisi gözden geçirilmeli<br>";
                if (farkYuzde >= 0)
                    html += "<strong>✅ DÜŞÜK ÖNCELİK:</strong> Mevcut stratejiye devam edilebilir<br>";
                if (Math.Abs(farkYuzde) < 2)
                    html += "<strong>📊 BİLGİ:</strong> Model ile uyumlu performans<br>";

                html += @"
    </div>
</div>";
            }

            html += @"
</body>
</html>";

            return html;
        }

        // Helper Methods
        private string GetLevelName(string level)
        {
            return level switch
            {
                "basic" => "📋 Basit Özet",
                "detailed" => "📊 Detaylı Analiz",
                "full" => "📈 Tam Rapor",
                _ => "Bilinmeyen"
            };
        }

        private async Task<List<dynamic>> GetTrendsDataForExport(string kurulus, string fon, string ihrac)
        {
            try
            {
                // GetTrends method'u IActionResult döndürüyor, biz data'ya ihtiyacımız var
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                var fonFilter = string.IsNullOrEmpty(fon) || fon == "All" || fon == "$__all" ? "" : " AND fon_no::text = @FonNo";
                var ihracFilter = string.IsNullOrEmpty(ihrac) || ihrac == "All" || ihrac == "$__all" ? "" : " AND ihrac_no::text = @IhracNo";

                // Basit trends query
                var sql = $@"
            SELECT 
                DATE_TRUNC('week', baslangic_tarihi) as timestamp,
                SUM(COALESCE(mevduat_tutari, 0)) as kumulatif_mevduat,
                SUM(COALESCE(faiz_tutari, 0)) as kumulatif_faiz_kazanci
            FROM nakit_akis 
            WHERE kaynak_kurulus = @KaynakKurulus
              AND baslangic_tarihi >= @FromDate
              AND baslangic_tarihi <= @ToDate
              {fonFilter}
              {ihracFilter}
            GROUP BY DATE_TRUNC('week', baslangic_tarihi)
            ORDER BY timestamp DESC
            LIMIT 10";

                var result = await connection.QueryAsync<dynamic>(sql, new
                {
                    KaynakKurulus = kurulus,
                    FonNo = fon,
                    IhracNo = ihrac,
                    FromDate = DateTime.Now.AddMonths(-6),
                    ToDate = DateTime.Now
                });

                return result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Trends data error: {ex.Message}");
                return new List<dynamic>();
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
                                         [FromQuery] string ihrac_no = null)
        {
            try
            {
                _logger.LogInformation("=== VARIABLE REQUEST DEBUG ===");
                _logger.LogInformation("Request method: {method}", HttpContext.Request.Method);
                _logger.LogInformation("Variable: {variable}", request?.Variable ?? variable);
                _logger.LogInformation("Request body params: {params}", request?.Params != null ? string.Join(", ", request.Params.Select(x => $"{x.Key}={x.Value}")) : "null");
                _logger.LogInformation("Query params - kaynak_kurulus: {kurulus}", kaynak_kurulus);
                _logger.LogInformation("Query params - fm_fonlar: {fonlar}", fm_fonlar);
                _logger.LogInformation("Query params - ihrac_no: {ihrac}", ihrac_no);

                var variableName = request?.Variable ?? variable;
                var kaynakKurulusParam = request?.Params?.GetValueOrDefault("kaynak_kurulus") ?? kaynak_kurulus;
                var fonParam = request?.Params?.GetValueOrDefault("fm_fonlar") ?? fm_fonlar;
                var ihracParam = request?.Params?.GetValueOrDefault("ihrac_no") ?? ihrac_no;

                _logger.LogInformation("Final params - Variable: {variable}, Kurulus: {kurulus}, Fon: {fon}, Ihrac: {ihrac}",
                    variableName, kaynakKurulusParam, fonParam, ihracParam);

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
                        var kurulus = kaynakKurulusParam ?? "FİBABANKA";
                        _logger.LogInformation("Getting fonlar for kurulus: '{kurulus}'", kurulus);
                        var fonlar = await _nakitAkisService.GetFonlarAsync(kurulus);
                        _logger.LogInformation("Found {count} fonlar for kurulus: {kurulus}", fonlar.Count, kurulus);

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
                        var kurulusIhrac = kaynakKurulusParam ?? "FİBABANKA";
                        var fonNoIhrac = fonParam;

                        _logger.LogInformation("Getting ihraclar for kurulus: '{kurulus}', fon: '{fon}'", kurulusIhrac, fonNoIhrac);

                        if (string.IsNullOrEmpty(fonNoIhrac))
                        {
                            _logger.LogWarning("Fon not selected for ihrac query");
                            return Ok(new[] { new { text = "Önce fon seçin", value = "" } });
                        }

                        try
                        {
                            var ihraclar = await _nakitAkisService.GetIhraclarAsync(kurulusIhrac, fonNoIhrac);
                            _logger.LogInformation("Found {count} ihraclar for fon: {fon}", ihraclar.Count, fonNoIhrac);

                            if (ihraclar.Any())
                            {
                                var ihracResult = ihraclar.Select(i => new
                                {
                                    text = $"İhraç {i.IhracNo} (₺{i.ToplamTutar:N0} - {i.KayitSayisi} kayıt)",
                                    value = i.IhracNo,
                                    tutar = i.ToplamTutar, 
                                    kayit = i.KayitSayisi  
                                }).ToList();

                                // Debug log ekle
                                foreach (var item in ihracResult)
                                {
                                    _logger.LogInformation("İhraç return: {text}, value: {value}, tutar: {tutar}",
                                        item.text, item.value, item.tutar);
                                }

                                return Ok(ihracResult);
                            }
                            else
                            {
                                return Ok(new[] { new { text = "Bu fon için ihraç bulunamadı", value = "" } });
                            }
                        }
                        catch (Exception ihracEx)
                        {
                            _logger.LogError(ihracEx, "Error getting ihraclar for kurulus: {kurulus}, fon: {fon}", kurulusIhrac, fonNoIhrac);
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

        
        private async Task<dynamic> GetAnalysisData(decimal faizOrani, string kaynak_kurulus, string fm_fonlar, string ihrac_no)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var fonFilter = !string.IsNullOrEmpty(fm_fonlar) && fm_fonlar != "All" && fm_fonlar != "$__all" ?
                " AND fon_no::text = @FonNo" : "";
            var ihracFilter = !string.IsNullOrEmpty(ihrac_no) && ihrac_no != "All" && ihrac_no != "$__all" ?
                " AND ihrac_no::text = @IhracNo" : "";

            var sql = $@"
        SELECT 
            SUM(COALESCE(mevduat_tutari, 0)) as toplam_mevduat,
            SUM(COALESCE(faiz_tutari, 0)) as gercek_faiz_tutari,
            COUNT(*) as toplam_islem,
            AVG(COALESCE(donus_tarihi - baslangic_tarihi, 30)) as ortalama_vade
        FROM nakit_akis 
        WHERE kaynak_kurulus = @KaynakKurulus
          AND COALESCE(mevduat_tutari, 0) > 0
          AND donus_tarihi > baslangic_tarihi
          {fonFilter}
          {ihracFilter}";

            var result = await connection.QueryFirstOrDefaultAsync(sql, new
            {
                KaynakKurulus = kaynak_kurulus,
                FonNo = fm_fonlar,
                IhracNo = ihrac_no
            });

            return result; // Direkt result döndür, IActionResult değil
        }

        private async Task<GrafanaQueryResult> ProcessTarget(GrafanaTarget target, GrafanaRange range)
        {
            // Default parametreler
            var parametreler = new NakitAkisParametre
            {
                FaizOrani = 0.45m,
                KaynakKurulus = "FİBABANKA"
            };

            // Target'dan parametreleri parse etme
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
