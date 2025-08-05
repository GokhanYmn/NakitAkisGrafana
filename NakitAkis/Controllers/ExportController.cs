using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NakitAkis.Models;
using NakitAkis.Services;

namespace NakitAkis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly INakitAkisService _nakitAkisService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            IExportService exportService,
            INakitAkisService nakitAkisService,
            ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _nakitAkisService = nakitAkisService;
            _logger = logger;
        }

        [HttpPost("excel")]
        public async Task<IActionResult> ExportToExcel([FromBody] ExportRequest request)
        {
            try
            {
                var sonuc = await _nakitAkisService.GetNakitAkisAnaliziAsync(request.Parametreler);
                var excelData = await _exportService.ExportToExcelAsync(sonuc, request.Parametreler);

                var fileName = $"nakit-akis-analizi-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";

                return File(excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel export failed");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("csv")]
        public async Task<IActionResult> ExportToCsv([FromBody] ExportRequest request)
        {
            try
            {
                var sonuc = await _nakitAkisService.GetNakitAkisAnaliziAsync(request.Parametreler);
                var csvData = await _exportService.ExportToCsvAsync(sonuc, request.Parametreler);

                var fileName = $"nakit-akis-analizi-{DateTime.Now:yyyyMMdd-HHmmss}.csv";

                return File(csvData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV export failed");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("html")]
        public async Task<IActionResult> ExportToHtml([FromBody] ExportRequest request)
        {
            try
            {
                var sonuc = await _nakitAkisService.GetNakitAkisAnaliziAsync(request.Parametreler);
                var html = await _exportService.GenerateReportHtmlAsync(sonuc, request.Parametreler);

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTML export failed");
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class ExportRequest
    {
        public NakitAkisParametre Parametreler { get; set; } = new();
    }
}

