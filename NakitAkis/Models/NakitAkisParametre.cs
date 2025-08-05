using System.ComponentModel.DataAnnotations;

namespace NakitAkis.Models
{
    public class NakitAkisParametre
    {
        [Required]
        [Range(0.01, 1.0, ErrorMessage = "Faiz oranı 0.01 ile 1.0 arasında olmalıdır")]
        public decimal FaizOrani { get; set; } = 0.45m;

        public string? KaynakKurulus { get; set; } = "FİBABANKA";
        public List<string> SecilenBankalar { get; set; } = new();
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
    }
}
