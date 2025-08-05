using NakitAkis.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace NakitAkis.Services
{
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger;
            // EPPlus lisans ayarı
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }

        public async Task<byte[]> ExportToExcelAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler)
        {
            try
            {
                using var package = new ExcelPackage();

                // Ana Sonuçlar Sayfası
                var worksheet = package.Workbook.Worksheets.Add("Nakit Akış Analizi");

                // Header
                worksheet.Cells["A1"].Value = "NAKİT AKIŞ ANALİZİ RAPORU";
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1:D1"].Merge = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Tarih
                worksheet.Cells["A2"].Value = $"Rapor Tarihi: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cells["A2:D2"].Merge = true;

                // Parametreler
                worksheet.Cells["A4"].Value = "PARAMETRELER";
                worksheet.Cells["A4"].Style.Font.Bold = true;
                worksheet.Cells["A4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A4"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

                worksheet.Cells["A5"].Value = "Faiz Oranı:";
                worksheet.Cells["B5"].Value = $"{parametreler.FaizOrani:P2}";

                worksheet.Cells["A6"].Value = "Kaynak Kuruluş:";
                worksheet.Cells["B6"].Value = parametreler.KaynakKurulus;

                if (parametreler.BaslangicTarihi.HasValue)
                {
                    worksheet.Cells["A7"].Value = "Başlangıç Tarihi:";
                    worksheet.Cells["B7"].Value = parametreler.BaslangicTarihi.Value.ToString("dd/MM/yyyy");
                }

                if (parametreler.BitisTarihi.HasValue)
                {
                    worksheet.Cells["A8"].Value = "Bitiş Tarihi:";
                    worksheet.Cells["B8"].Value = parametreler.BitisTarihi.Value.ToString("dd/MM/yyyy");
                }

                // Sonuçlar
                worksheet.Cells["A10"].Value = "SONUÇLAR";
                worksheet.Cells["A10"].Style.Font.Bold = true;
                worksheet.Cells["A10"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A10"].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

                worksheet.Cells["A12"].Value = "Metrik";
                worksheet.Cells["B12"].Value = "Tutar (₺)";
                worksheet.Cells["C12"].Value = "Yüzde (%)";

                // Header formatı
                worksheet.Cells["A12:C12"].Style.Font.Bold = true;
                worksheet.Cells["A12:C12"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A12:C12"].Style.Fill.BackgroundColor.SetColor(Color.Gray);
                worksheet.Cells["A12:C12"].Style.Font.Color.SetColor(Color.White);

                // Veriler
                worksheet.Cells["A13"].Value = "Toplam Faiz Tutarı";
                worksheet.Cells["B13"].Value = sonuc.ToplamFaizTutari;
                worksheet.Cells["B13"].Style.Numberformat.Format = "#,##0.00";

                worksheet.Cells["A14"].Value = "Model Faiz Tutarı";
                worksheet.Cells["B14"].Value = sonuc.ToplamModelFaizTutari;
                worksheet.Cells["B14"].Style.Numberformat.Format = "#,##0.00";

                worksheet.Cells["A15"].Value = "Fark Tutarı";
                worksheet.Cells["B15"].Value = sonuc.FarkTutari;
                worksheet.Cells["B15"].Style.Numberformat.Format = "#,##0.00";

                worksheet.Cells["A16"].Value = "Fark Yüzdesi";
                worksheet.Cells["C16"].Value = sonuc.FarkYuzdesi / 100;
                worksheet.Cells["C16"].Style.Numberformat.Format = "0.00%";

                // Otomatik genişlik ayarı
                worksheet.Cells.AutoFitColumns();

                // Grafik sayfası
                var chartSheet = package.Workbook.Worksheets.Add("Grafik Verileri");
                chartSheet.Cells["A1"].Value = "Metrik";
                chartSheet.Cells["B1"].Value = "Değer";

                chartSheet.Cells["A2"].Value = "Toplam Faiz";
                chartSheet.Cells["B2"].Value = sonuc.ToplamFaizTutari;

                chartSheet.Cells["A3"].Value = "Model Faiz";
                chartSheet.Cells["B3"].Value = sonuc.ToplamModelFaizTutari;

                chartSheet.Cells["A4"].Value = "Fark";
                chartSheet.Cells["B4"].Value = Math.Abs(sonuc.FarkTutari);

                return await Task.FromResult(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel export failed");
                throw;
            }
        }

        public async Task<byte[]> ExportToCsvAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler)
        {
            try
            {
                var csv = new StringBuilder();

                // Header
                csv.AppendLine("Nakit Akış Analizi Raporu");
                csv.AppendLine($"Rapor Tarihi,{DateTime.Now:dd/MM/yyyy HH:mm}");
                csv.AppendLine();

                // Parametreler
                csv.AppendLine("PARAMETRELER");
                csv.AppendLine($"Faiz Oranı,{parametreler.FaizOrani:P2}");
                csv.AppendLine($"Kaynak Kuruluş,{parametreler.KaynakKurulus}");

                if (parametreler.BaslangicTarihi.HasValue)
                    csv.AppendLine($"Başlangıç Tarihi,{parametreler.BaslangicTarihi.Value:dd/MM/yyyy}");

                if (parametreler.BitisTarihi.HasValue)
                    csv.AppendLine($"Bitiş Tarihi,{parametreler.BitisTarihi.Value:dd/MM/yyyy}");

                csv.AppendLine();

                // Sonuçlar
                csv.AppendLine("SONUÇLAR");
                csv.AppendLine("Metrik,Tutar (₺),Yüzde (%)");
                csv.AppendLine($"Toplam Faiz Tutarı,{sonuc.ToplamFaizTutari:N2},");
                csv.AppendLine($"Model Faiz Tutarı,{sonuc.ToplamModelFaizTutari:N2},");
                csv.AppendLine($"Fark Tutarı,{sonuc.FarkTutari:N2},");
                csv.AppendLine($"Fark Yüzdesi,,{sonuc.FarkYuzdesi:N2}%");

                return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV export failed");
                throw;
            }
        }

        public async Task<byte[]> ExportToPdfAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler)
        {
            try
            {
                // HTML template oluştur
                var html = await GenerateReportHtmlAsync(sonuc, parametreler);

                // Bu kısım için iTextSharp veya PuppeteerSharp kullanılabilir
                // Şimdilik HTML olarak dönelim, sonra PDF'e çeviririz
                return Encoding.UTF8.GetBytes(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF export failed");
                throw;
            }
        }

        public async Task<string> GenerateReportHtmlAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Nakit Akış Analizi Raporu</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .parameters {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 30px; }}
        .results {{ margin-bottom: 30px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #007bff; color: white; }}
        .metric-positive {{ color: #28a745; }}
        .metric-negative {{ color: #dc3545; }}
        .footer {{ text-align: center; margin-top: 50px; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>NAKİT AKIŞ ANALİZİ RAPORU</h1>
        <p>Rapor Tarihi: {DateTime.Now:dd/MM/yyyy HH:mm}</p>
    </div>

    <div class='parameters'>
        <h2>Analiz Parametreleri</h2>
        <p><strong>Faiz Oranı:</strong> {parametreler.FaizOrani:P2}</p>
        <p><strong>Kaynak Kuruluş:</strong> {parametreler.KaynakKurulus}</p>
        {(parametreler.BaslangicTarihi.HasValue ? $"<p><strong>Başlangıç Tarihi:</strong> {parametreler.BaslangicTarihi.Value:dd/MM/yyyy}</p>" : "")}
        {(parametreler.BitisTarihi.HasValue ? $"<p><strong>Bitiş Tarihi:</strong> {parametreler.BitisTarihi.Value:dd/MM/yyyy}</p>" : "")}
        {(parametreler.SecilenBankalar.Any() ? $"<p><strong>Seçilen Bankalar:</strong> {string.Join(", ", parametreler.SecilenBankalar)}</p>" : "")}
    </div>

    <div class='results'>
        <h2>Analiz Sonuçları</h2>
        <table>
            <thead>
                <tr>
                    <th>Metrik</th>
                    <th>Tutar (₺)</th>
                    <th>Yüzde (%)</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Toplam Faiz Tutarı</td>
                    <td>₺{sonuc.ToplamFaizTutari:N2}</td>
                    <td>-</td>
                </tr>
                <tr>
                    <td>Model Faiz Tutarı</td>
                    <td>₺{sonuc.ToplamModelFaizTutari:N2}</td>
                    <td>-</td>
                </tr>
                <tr class='{(sonuc.FarkTutari >= 0 ? "metric-positive" : "metric-negative")}'>
                    <td>Fark Tutarı</td>
                    <td>₺{sonuc.FarkTutari:N2}</td>
                    <td>-</td>
                </tr>
                <tr class='{(sonuc.FarkYuzdesi >= 0 ? "metric-positive" : "metric-negative")}'>
                    <td>Fark Yüzdesi</td>
                    <td>-</td>
                    <td>{sonuc.FarkYuzdesi:N2}%</td>
                </tr>
            </tbody>
        </table>
    </div>

    <div class='footer'>
        <p>Bu rapor Nakit Akış Dashboard sistemi tarafından otomatik olarak oluşturulmuştur.</p>
    </div>
</body>
</html>";

            return await Task.FromResult(html);
        }
    }
}

