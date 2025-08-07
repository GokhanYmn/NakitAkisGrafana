using NakitAkis.Models;

namespace NakitAkis.Services
{
    public interface INakitAkisService
    {
        Task<NakitAkisSonuc> GetNakitAkisAnaliziAsync(NakitAkisParametre parametre);
        Task<List<BankaBilgi>> GetBankalarAsync();
        Task<List<KaynakKurulusBilgi>> GetKaynakKuruluslarAsync();
        Task<List<NakitAkisHistoricalData>> GetHistoricalDataAsync(NakitAkisParametre parametre,DateTime fromDate,DateTime toDate);
        Task<bool> TestConnectionAsync();

        Task<List<FonBilgi>> GetFonlarAsync(string kaynakKurulus);

        Task<List<IhracBilgi>> GetIhraclarAsync(string kaynakKurulus, string fonNo);
    }
}
