using NakitAkis.Models;

namespace NakitAkis.Services
{
    public interface INakitAkisService
    {
        Task<NakitAkisSonuc> GetNakitAkisAnaliziAsync(NakitAkisParametre parametre);
        Task<List<BankaBilgi>> GetBankalarAsync();
        Task<List<KaynakKurulusBilgi>> GetKaynakKuruluslarAsync();
        Task<bool> TestConnectionAsync();
    }
}
