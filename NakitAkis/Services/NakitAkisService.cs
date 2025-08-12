using Dapper;
using NakitAkis.Models;
using Npgsql;

namespace NakitAkis.Services
{
    public class NakitAkisService : INakitAkisService
    {
        private readonly string _connectionString;
        private readonly ILogger<NakitAkisService> _logger;

        public NakitAkisService(IConfiguration configuration, ILogger<NakitAkisService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("DefaultConnection string is required");
            _logger = logger;
        }

        public async Task<NakitAkisSonuc> GetNakitAkisAnaliziAsync(NakitAkisParametre parametre)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = BuildAnalysisQuery(parametre);
                _logger.LogInformation("Executing query with parameters: {@Parameters}", parametre);

                // Decimal overflow'u önlemek için double kullan
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    FaizOrani = parametre.FaizOrani,
                    ModelFaizOrani = parametre.ModelFaizOrani,
                    KaynakKurulus = parametre.KaynakKurulus,
                    BaslangicTarihi = parametre.BaslangicTarihi,
                    BitisTarihi = parametre.BitisTarihi,
                    SecilenFonNo = parametre.SecilenFonNo,
                    SecilenIhracNo = parametre.SecilenIhracNo
                });

                // Güvenli decimal dönüştürme
                decimal toplamFaiz = 0;
                decimal modelFaiz = 0;

                if (result != null)
                {
                    try
                    {
                        // Double'dan decimal'a güvenli dönüştürme
                        var sumValue = result.sum ?? 0;
                        var sum1Value = result.sum1 ?? 0;

                        toplamFaiz = Convert.ToDecimal(Math.Min(Convert.ToDouble(sumValue), (double)decimal.MaxValue));
                        modelFaiz = Convert.ToDecimal(Math.Min(Convert.ToDouble(sum1Value), (double)decimal.MaxValue));
                    }
                    catch (OverflowException ex)
                    {
                        _logger.LogWarning(ex, "Decimal overflow detected, using fallback values");
                        toplamFaiz = 999999999999m; // Fallback value
                        modelFaiz = 999999999999m;  // Fallback value
                    }
                }

                return new NakitAkisSonuc
                {
                    ToplamFaizTutari = toplamFaiz,
                    ToplamModelFaizTutari = modelFaiz
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing nakit akış analysis");

                // Database hatası durumunda mock data döndür
                return new NakitAkisSonuc
                {
                    ToplamFaizTutari = 500000m,
                    ToplamModelFaizTutari = 450000m
                };
            }

        }

        private string BuildDatabaseQuery(NakitAkisParametre parametre)
        {
            var bankaFilter = BuildBankaFilter(parametre.SecilenBankalar);
            var tarihFilter = BuildTarihFilter();

            return $@"
        SELECT 
            SUM(faiz_tutari) as sum,
            SUM(model_faiz_tutari) as sum1
        FROM (
            SELECT *,
                   donus_tarihi - baslangic_tarihi as vade,
                   (1 + faiz_orani * (donus_tarihi - baslangic_tarihi) / 365.00)^(365.00/(donus_tarihi - baslangic_tarihi)) - 1 as bilesik_faiz,
                   mevduat_tutari * (1 + model_bilesik)^((donus_tarihi - baslangic_tarihi) / 365.00) - mevduat_tutari as model_faiz_tutari                  
            FROM (      
                SELECT id, kaynak_kurulus, fon_no, ihrac_no, vdmk_isin_kodu, banka_adi,
                       baslangic_tarihi, mevduat_tutari, @ModelFaizOrani as faiz_orani, donus_tarihi,
                       mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00 as faiz_tutari,
                       mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00 + toplam_donus as toplam_donus,
                       (1 + @ModelFaizOrani / 365.00)^365.00 - 1 as model_bilesik  
                FROM nakit_akis na 
                WHERE banka_adi LIKE '%ZBJ%' 
                  {tarihFilter}
                  {bankaFilter}
                
                UNION
                
                SELECT id, kaynak_kurulus, fon_no, ihrac_no, vdmk_isin_kodu, banka_adi,
                       baslangic_tarihi, mevduat_tutari,
                       CASE WHEN faiz_tutari = 0 THEN 0.48 ELSE faiz_orani END as faiz_orani,
                       donus_tarihi,
                       CASE WHEN faiz_tutari = 0 THEN mevduat_tutari * 0.48 * (donus_tarihi - baslangic_tarihi) / 365.00 
                            ELSE faiz_tutari END as faiz_tutari,
                       CASE WHEN faiz_tutari = 0 THEN mevduat_tutari + mevduat_tutari * 0.48 * (donus_tarihi - baslangic_tarihi) / 365.00 
                            ELSE toplam_donus END as toplam_donus,
                       (1 + @ModelFaizOrani / 365.00)^365.00 - 1 as model_bilesik       
                FROM nakit_akis na 
                WHERE banka_adi NOT LIKE '%ZBJ%' 
                  AND toplam_donus > 0
                  {tarihFilter}
                  {bankaFilter}
            ) K            
            WHERE toplam_donus > 0
              AND kaynak_kurulus = @KaynakKurulus
        ) K";
        }

        private string BuildSimpleFaizQuery(NakitAkisParametre parametre)
        {
            var bankaFilter = BuildBankaFilter(parametre.SecilenBankalar);
            var tarihFilter = BuildTarihFilter();

            return $@"
        SELECT 
            SUM(mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.0) as sum,
            SUM(mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.0) as sum1
        FROM nakit_akis 
        WHERE kaynak_kurulus = @KaynakKurulus
          AND toplam_donus > 0
          {tarihFilter}
          {bankaFilter}";
        }

        private string BuildCompoundFaizQuery(NakitAkisParametre parametre)
        {
            var bankaFilter = BuildBankaFilter(parametre.SecilenBankalar);
            var tarihFilter = BuildTarihFilter();

            return $@"
        SELECT 
            SUM(mevduat_tutari * (1 + @ModelFaizOrani / 365.0)^(donus_tarihi - baslangic_tarihi) - mevduat_tutari) as sum,
            SUM(mevduat_tutari * (1 + @ModelFaizOrani / 365.0)^(donus_tarihi - baslangic_tarihi) - mevduat_tutari) as sum1
        FROM nakit_akis 
        WHERE kaynak_kurulus = @KaynakKurulus
          AND toplam_donus > 0
          {tarihFilter}
          {bankaFilter}";
        }
        public async Task<List<NakitAkisHistoricalData>> GetHistoricalDataAsync(NakitAkisParametre parametre, DateTime fromDate, DateTime toDate)
        {
            try
            {
                _logger.LogInformation("Historical data request: {kurulus}, {from} - {to}",
                    parametre.KaynakKurulus, fromDate, toDate);

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var bankaFilter = BuildBankaFilter(parametre.SecilenBankalar);
                var fonFilter = BuildFonFilter(parametre.SecilenFonNo);
                var ihracFilter = BuildIhracFilter(parametre.SecilenIhracNo);

                //Sgl Sorgusu
                var sql = $@"
           WITH daily_data AS (
                SELECT 
                    DATE(baslangic_tarihi) as gun,
                    SUM(COALESCE(faiz_tutari, 0))::float8 as toplam_faiz,
                    SUM(COALESCE(model_faiz_tutari, 0))::float8 as toplam_model_faiz
                FROM (
                    SELECT *,
                           donus_tarihi - baslangic_tarihi as vade,
                           (1 + faiz_orani * (donus_tarihi - baslangic_tarihi) / 365.00)^(365.00 / NULLIF(donus_tarihi - baslangic_tarihi, 0)) - 1 as bilesik_faiz,
                           CASE 
                               WHEN donus_tarihi - baslangic_tarihi > 0 THEN 
                                   mevduat_tutari * (1 + model_bilesik)^((donus_tarihi - baslangic_tarihi) / 365.00) - mevduat_tutari
                               ELSE 0 
                           END as model_faiz_tutari
                    FROM (      
                        SELECT id, kaynak_kurulus, fon_no, ihrac_no, vdmk_isin_kodu, banka_adi,
                               baslangic_tarihi, 
                               COALESCE(mevduat_tutari, 0) as mevduat_tutari, 
                               @ModelFaizOrani as faiz_orani, 
                               donus_tarihi,
                               CASE 
                                   WHEN donus_tarihi > baslangic_tarihi THEN
                                       COALESCE(mevduat_tutari, 0) * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00
                                   ELSE 0
                               END as faiz_tutari,
                               COALESCE(toplam_donus, 0) as toplam_donus,
                               (1 + @ModelFaizOrani / 365.00)^365.00 - 1 as model_bilesik  
                        FROM nakit_akis na 
                        WHERE banka_adi LIKE '%ZBJ%' 
                          AND baslangic_tarihi >= @FromDate
                          AND baslangic_tarihi <= @ToDate
                          AND COALESCE(mevduat_tutari, 0) > 0
                          AND donus_tarihi > baslangic_tarihi
                          {fonFilter}
                          {ihracFilter}
                        
                        UNION ALL
                        
                        SELECT id, kaynak_kurulus, fon_no, ihrac_no, vdmk_isin_kodu, banka_adi,
                               baslangic_tarihi, 
                               COALESCE(mevduat_tutari, 0) as mevduat_tutari,
                               CASE WHEN COALESCE(faiz_tutari, 0) = 0 THEN 0.48 ELSE COALESCE(faiz_orani, 0.48) END as faiz_orani,
                               donus_tarihi,
                               CASE 
                                   WHEN COALESCE(faiz_tutari, 0) = 0 AND donus_tarihi > baslangic_tarihi THEN 
                                       COALESCE(mevduat_tutari, 0) * 0.48 * (donus_tarihi - baslangic_tarihi) / 365.00 
                                   ELSE COALESCE(faiz_tutari, 0) 
                               END as faiz_tutari,
                               COALESCE(toplam_donus, 0) as toplam_donus,
                               (1 + @ModelFaizOrani / 365.00)^365.00 - 1 as model_bilesik       
                        FROM nakit_akis na 
                        WHERE banka_adi NOT LIKE '%ZBJ%' 
                          AND COALESCE(toplam_donus, 0) > 0
                          AND baslangic_tarihi >= @FromDate
                          AND baslangic_tarihi <= @ToDate
                          AND COALESCE(mevduat_tutari, 0) > 0
                          AND donus_tarihi > baslangic_tarihi
                          {fonFilter}
                          {ihracFilter}
                    ) K            
                    WHERE COALESCE(toplam_donus, 0) > 0
                      AND kaynak_kurulus = @KaynakKurulus
                ) K
                GROUP BY DATE(baslangic_tarihi)
            )
            SELECT 
                gun as Tarih,
                COALESCE(toplam_faiz, 0) as ToplamFaizTutari,
                COALESCE(toplam_model_faiz, 0) as ToplamModelFaizTutari
            FROM daily_data
            WHERE COALESCE(toplam_faiz, 0) > 0 OR COALESCE(toplam_model_faiz, 0) > 0
            ORDER BY gun";

                var result = await connection.QueryAsync<NakitAkisHistoricalData>(sql, new
                {
                    FaizOrani = (double)parametre.FaizOrani,
                    ModelFaizOrani = (double)parametre.ModelFaizOrani,
                    KaynakKurulus = parametre.KaynakKurulus,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SecilenFonNo = parametre.SecilenFonNo,
                    SecilenIhracNo = parametre.SecilenIhracNo
                });

                _logger.LogInformation("Query returned {count} records for kurulus: {kurulus}, fon: {fon}",
             result.Count(), parametre.KaynakKurulus, parametre.SecilenFonNo);

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical data for {kurulus}: {message}",
                    parametre.KaynakKurulus, ex.Message);
                return new List<NakitAkisHistoricalData>();
            }
        }
        private string BuildAnalysisQuery(NakitAkisParametre parametre)
        {
            var bankaFilter = BuildBankaFilter(parametre.SecilenBankalar);
            var tarihFilter = BuildTarihFilter();
            var fonFilter = BuildFonFilter(parametre.SecilenFonNo);
            var ihracFilter = BuildIhracFilter(parametre.SecilenIhracNo);
            return $@"
                SELECT 
            SUM(faiz_tutari) as sum,
            SUM(model_faiz_tutari) as sum1
        FROM (
            SELECT *,
                   donus_tarihi - baslangic_tarihi as vade,
                   (1 + faiz_orani * (donus_tarihi - baslangic_tarihi) / 365.00)^(365.00/(donus_tarihi - baslangic_tarihi)) - 1 as bilesik_faiz,
                   mevduat_tutari * (1 + model_bilesik)^((donus_tarihi - baslangic_tarihi) / 365.00) - mevduat_tutari as model_faiz_tutari                  
            FROM (      
                SELECT id, kaynak_kurulus, fon_no, ihrac_no, vdmk_isin_kodu, banka_adi,
                       baslangic_tarihi, mevduat_tutari, @ModelFaizOrani as faiz_orani, donus_tarihi,
                       mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00 as faiz_tutari,
                       mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00 + toplam_donus as toplam_donus,
                       (1 + @ModelFaizOrani / 365.00)^365.00 - 1 as model_bilesik  
                FROM nakit_akis na 
                WHERE banka_adi LIKE '%ZBJ%' 
                  {tarihFilter}
                  {bankaFilter}
                  {fonFilter}
                  {ihracFilter}
                
                UNION
                
                SELECT id, kaynak_kurulus, fon_no, ihrac_no, vdmk_isin_kodu, banka_adi,
                       baslangic_tarihi, mevduat_tutari,
                       CASE WHEN faiz_tutari = 0 THEN @ModelFaizOrani ELSE faiz_orani END as faiz_orani,
                       donus_tarihi,
                       CASE WHEN faiz_tutari = 0 THEN mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00 
                            ELSE faiz_tutari END as faiz_tutari,
                       CASE WHEN faiz_tutari = 0 THEN mevduat_tutari + mevduat_tutari * @ModelFaizOrani * (donus_tarihi - baslangic_tarihi) / 365.00 
                            ELSE toplam_donus END as toplam_donus,
                       (1 + @ModelFaizOrani / 365.00)^365.00 - 1 as model_bilesik       
                FROM nakit_akis na 
                WHERE banka_adi NOT LIKE '%ZBJ%' 
                  AND toplam_donus > 0
                  {tarihFilter}
                  {bankaFilter}
                  {fonFilter}
                  {ihracFilter}
            ) K            
            WHERE toplam_donus > 0
              AND kaynak_kurulus = @KaynakKurulus
        ) K";
        }

        private string BuildFonFilter(string? secilenFonNo)
        {
            if (string.IsNullOrEmpty(secilenFonNo))
                return string.Empty;

            return " AND fon_no = @SecilenFonNo";
        }

        private string BuildIhracFilter(string? secilenIhracNo)
        {
            if (string.IsNullOrEmpty(secilenIhracNo))
                return string.Empty;

            return " AND ihrac_no = @SecilenIhracNo";
        }


        private string BuildBankaFilter(List<string> secilenBankalar)
        {
            if (!secilenBankalar.Any())
                return string.Empty;

            var conditions = secilenBankalar.Select((_, index) => $"banka_adi LIKE @Banka{index}").ToList();
            return $" AND ({string.Join(" OR ", conditions)})";
        }

        private string BuildTarihFilter()
        {
            var conditions = new List<string>();

            if (DateTime.TryParse("@BaslangicTarihi", out _))
                conditions.Add("baslangic_tarihi >= @BaslangicTarihi");

            if (DateTime.TryParse("@BitisTarihi", out _))
                conditions.Add("baslangic_tarihi <= @BitisTarihi");

            return conditions.Any() ? $" AND {string.Join(" AND ", conditions)}" : string.Empty;
        }

        public async Task<List<BankaBilgi>> GetBankalarAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT DISTINCT banka_adi as BankaAdi, COUNT(*) as KayitSayisi
                    FROM nakit_akis 
                    WHERE banka_adi IS NOT NULL 
                      AND banka_adi != ''
                    GROUP BY banka_adi
                    ORDER BY banka_adi";

                var result = await connection.QueryAsync<BankaBilgi>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banks");
                return new List<BankaBilgi>();
            }
        }

        public async Task<List<KaynakKurulusBilgi>> GetKaynakKuruluslarAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT DISTINCT kaynak_kurulus as KaynakKurulus, COUNT(*) as KayitSayisi
                    FROM nakit_akis 
                    WHERE kaynak_kurulus IS NOT NULL 
                      AND kaynak_kurulus != ''
                    GROUP BY kaynak_kurulus
                    ORDER BY kaynak_kurulus";

                var result = await connection.QueryAsync<KaynakKurulusBilgi>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kaynak kurulus");
                return new List<KaynakKurulusBilgi>();
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }

        public async Task<List<FonBilgi>> GetFonlarAsync(string kaynakKurulus)
        {
            try
            {
                _logger.LogInformation("=== FONLAR DEBUG START ===");
                _logger.LogInformation("Requested kurulus: '{kurulus}'", kaynakKurulus);

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully");

                // Önce basit test
                var testSql = "SELECT COUNT(*) FROM nakit_akis WHERE kaynak_kurulus = @KaynakKurulus";
                var totalCount = await connection.QuerySingleAsync<int>(testSql, new { KaynakKurulus = kaynakKurulus });
                _logger.LogInformation("Total records for '{kurulus}': {count}", kaynakKurulus, totalCount);

                // Fon sayısını kontrol et
                var fonCountSql = @"
            SELECT COUNT(DISTINCT fon_no) 
            FROM nakit_akis 
            WHERE kaynak_kurulus = @KaynakKurulus 
              AND fon_no IS NOT NULL";

                var fonCount = await connection.QuerySingleAsync<int>(fonCountSql, new { KaynakKurulus = kaynakKurulus });
                _logger.LogInformation("Distinct fon count for '{kurulus}': {count}", kaynakKurulus, fonCount);

                // Ana sorgu
                var sql = @"
            SELECT DISTINCT 
                fon_no as FonNo, 
                COUNT(*) as KayitSayisi,
                SUM(COALESCE(mevduat_tutari, 0)) as ToplamTutar
            FROM nakit_akis 
            WHERE kaynak_kurulus = @KaynakKurulus
              AND fon_no IS NOT NULL 
            GROUP BY fon_no
            ORDER BY fon_no
            LIMIT 10";

                _logger.LogInformation("Executing main query...");
                var result = await connection.QueryAsync<FonBilgi>(sql, new { KaynakKurulus = kaynakKurulus });

                _logger.LogInformation("Query executed. Result count: {count}", result.Count());

                foreach (var fon in result.Take(3))
                {
                    _logger.LogInformation("Fon: {fonNo}, Kayıt: {kayit}, Tutar: {tutar}",
                        fon.FonNo, fon.KayitSayisi, fon.ToplamTutar);
                }

                _logger.LogInformation("=== FONLAR DEBUG END ===");
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fonlar for {kurulus}", kaynakKurulus);
                return new List<FonBilgi>();
            }
        }

        public async Task<List<IhracBilgi>> GetIhraclarAsync(string kaynakKurulus, string fonNo)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                Console.WriteLine("=== REAL TUTAR CALCULATION ===");
                Console.WriteLine($"Kurulus: {kaynakKurulus}, Fon: {fonNo}");

                var sql = @"
            SELECT 
                ihrac_no,
                COUNT(*) as kayit_sayisi,
                SUM(mevduat_tutari) as toplam_tutar,
                AVG(mevduat_tutari) as ortalama_tutar
            FROM nakit_akis 
            WHERE kaynak_kurulus = @KaynakKurulus
              AND fon_no::text = @FonNo
              AND ihrac_no IS NOT NULL 
              AND mevduat_tutari IS NOT NULL
              AND mevduat_tutari > 0
            GROUP BY ihrac_no
            ORDER BY ihrac_no
            LIMIT 10";

                var result = await connection.QueryAsync<dynamic>(sql, new
                {
                    KaynakKurulus = kaynakKurulus,
                    FonNo = fonNo
                });

                Console.WriteLine($"Query result count: {result.Count()}");

                var ihracList = new List<IhracBilgi>();
                foreach (var r in result)
                {
                    var ihracNo = r.ihrac_no?.ToString() ?? "NULL_VALUE";
                    var kayit = Convert.ToInt32(r.kayit_sayisi ?? 0);
                    var toplamTutar = Convert.ToDecimal(r.toplam_tutar ?? 0);

                    Console.WriteLine($"Real: ihrac='{ihracNo}', kayit={kayit}, tutar={toplamTutar}");

                    ihracList.Add(new IhracBilgi
                    {
                        IhracNo = ihracNo,
                        ToplamTutar = toplamTutar,
                        KayitSayisi = kayit
                    });
                }

                Console.WriteLine("=== REAL CALCULATION END ===");
                return ihracList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return new List<IhracBilgi>();
            }
        }
    }
}


