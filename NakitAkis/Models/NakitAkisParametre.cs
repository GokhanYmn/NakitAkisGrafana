using System.ComponentModel.DataAnnotations;

namespace NakitAkis.Models
{
    public class NakitAkisParametre
    {
        [Required]
        [Range(0.01, 50.0, ErrorMessage = "Faiz oranı 0.01 ile 50.0 arasında olmalıdır")]
        public decimal FaizOrani { get; set; } = 0.45m;
        [Range(0.01, 50.0)]
        public decimal ModelFaizOrani { get; set; } = 0.45m;
        public string? KaynakKurulus { get; set; } = null;
        public string? HesaplamaYontemi { get; set; } = "database";
        public List<string> SecilenBankalar { get; set; } = new();
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }

        public string? SecilenFonNo {  get; set; }
        public string? SecilenIhracNo { get; set; }
    }
}
