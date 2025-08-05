using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NakitAkis.Models;
using NakitAkis.Services;

namespace NakitAkis.Controller
{
    
        [ApiController]
        [Route("api/[controller]")]
        public class NakitAkisController : ControllerBase
        {
            private readonly INakitAkisService _nakitAkisService;
            private readonly ILogger<NakitAkisController> _logger;

            public NakitAkisController(INakitAkisService nakitAkisService, ILogger<NakitAkisController> logger)
            {
                _nakitAkisService = nakitAkisService;
                _logger = logger;
            }

            [HttpGet("analiz")]
            public async Task<IActionResult> GetAnaliz(
                [FromQuery] decimal faizOrani = 0.45m,
                [FromQuery] string kaynakKurulus = "FİBABANKA",
                [FromQuery] string? bankalar = null,
                [FromQuery] DateTime? baslangicTarihi = null,
                [FromQuery] DateTime? bitisTarihi = null)
            {
                try
                {
                    var parametreler = new NakitAkisParametre
                    {
                        FaizOrani = faizOrani,
                        KaynakKurulus = kaynakKurulus,
                        SecilenBankalar = string.IsNullOrEmpty(bankalar) ?
                            new List<string>() :
                            bankalar.Split(',').ToList(),
                        BaslangicTarihi = baslangicTarihi,
                        BitisTarihi = bitisTarihi
                    };

                    var sonuc = await _nakitAkisService.GetNakitAkisAnaliziAsync(parametreler);

                    // Grafana için uygun format
                    return Ok(new
                    {
                        columns = new[]
                        {
                        new { text = "Metric", type = "string" },
                        new { text = "Value", type = "number" },
                        new { text = "Time", type = "time" }
                    },
                        rows = new[]
                        {
                        new object[] { "Toplam Faiz Tutarı", sonuc.ToplamFaizTutari, DateTime.UtcNow },
                        new object[] { "Model Faiz Tutarı", sonuc.ToplamModelFaizTutari, DateTime.UtcNow },
                        new object[] { "Fark Tutarı", sonuc.FarkTutari, DateTime.UtcNow },
                        new object[] { "Fark Yüzdesi", sonuc.FarkYuzdesi, DateTime.UtcNow }
                    }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API Error in GetAnaliz");
                    return BadRequest(new { error = ex.Message });
                }
            }

            [HttpGet("bankalar")]
            public async Task<IActionResult> GetBankalar()
            {
                try
                {
                    var bankalar = await _nakitAkisService.GetBankalarAsync();
                    return Ok(bankalar);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API Error in GetBankalar");
                    return BadRequest(new { error = ex.Message });
                }
            }

            [HttpGet("kaynak-kuruluslar")]
            public async Task<IActionResult> GetKaynakKuruluslar()
            {
                try
                {
                    var kuruluslar = await _nakitAkisService.GetKaynakKuruluslarAsync();
                    return Ok(kuruluslar);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API Error in GetKaynakKuruluslar");
                    return BadRequest(new { error = ex.Message });
                }
            }

            [HttpPost("test-connection")]
            public async Task<IActionResult> TestConnection()
            {
                try
                {
                    var isConnected = await _nakitAkisService.TestConnectionAsync();
                    return Ok(new { connected = isConnected });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API Error in TestConnection");
                    return BadRequest(new { error = ex.Message });
                }
            }
        }
    }


