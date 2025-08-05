using NakitAkis.Models;

namespace NakitAkis.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportToExcelAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler);
        Task<byte[]> ExportToPdfAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler);
        Task<byte[]> ExportToCsvAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler);
        Task<string> GenerateReportHtmlAsync(NakitAkisSonuc sonuc, NakitAkisParametre parametreler);
    }
}
