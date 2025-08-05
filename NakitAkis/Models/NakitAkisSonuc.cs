namespace NakitAkis.Models
{
    public class NakitAkisSonuc
    {
        public decimal ToplamFaizTutari { get; set; }
        public decimal ToplamModelFaizTutari { get; set; }
        public decimal FarkTutari => ToplamFaizTutari - ToplamModelFaizTutari;
        public decimal FarkYuzdesi => ToplamModelFaizTutari != 0 ? (FarkTutari / ToplamModelFaizTutari) * 100 : 0;
    }
}
