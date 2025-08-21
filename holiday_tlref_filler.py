import pandas as pd
import psycopg2
from datetime import datetime, timedelta
import time

# -------------------------------
# AYARLAR
# -------------------------------
DB_CONFIG = {
    "host":"192.168.182.3","dbname":"tmks-ftp","user":"postgres","password":"postgres.db!"
}

def get_missing_tlref_dates():
    """TLREF faizi olmayan tarihleri bul"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # TLREF faizi olmayan kayıtları bul
        cur.execute("""
            SELECT tarih, anapara
            FROM cash_flow_analysis 
            WHERE tlref_faiz IS NULL 
            ORDER BY tarih
        """)
        
        missing_dates = cur.fetchall()
        
        cur.close()
        conn.close()
        
        print(f"TLREF faizi olmayan {len(missing_dates)} tarih bulundu:")
        for date_val, anapara in missing_dates[:10]:  # İlk 10'unu göster
            print(f"  {date_val}: Anapara {anapara:,.0f}")
        
        if len(missing_dates) > 10:
            print(f"  ... ve {len(missing_dates) - 10} tane daha")
            
        return missing_dates
        
    except Exception as e:
        print(f"Eksik tarihleri alma hatası: {e}")
        return []

def find_previous_workday_tlref(target_date):
    """Belirli bir tarih için önceki işgününün TLREF değerini bul"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Önceki işgününün TLREF değerini ara (maksimum 10 gün geriye)
        search_date = target_date
        for i in range(10):
            search_date = search_date - timedelta(days=1)
            
            # Bu tarihte TLREF değeri var mı?
            cur.execute("""
                SELECT tlref_faiz 
                FROM cash_flow_analysis 
                WHERE tarih = %s AND tlref_faiz IS NOT NULL
            """, (search_date,))
            
            result = cur.fetchone()
            if result:
                tlref_value = float(result[0])
                cur.close()
                conn.close()
                return search_date, tlref_value
        
        cur.close()
        conn.close()
        return None, None
        
    except Exception as e:
        print(f"Önceki işgünü TLREF arama hatası: {e}")
        return None, None

def fill_holiday_tlref():
    """Tatil günlerini önceki işgününün TLREF değeri ile doldur"""
    missing_dates = get_missing_tlref_dates()
    
    if not missing_dates:
        print("Tüm tarihlerde TLREF değeri mevcut.")
        return
    
    print(f"\n{len(missing_dates)} eksik tarih için önceki işgünü TLREF değerleri aranıyor...")
    
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        updated_count = 0
        not_found_count = 0
        
        for date_val, anapara in missing_dates:
            # Önceki işgününün TLREF değerini bul
            prev_date, prev_tlref = find_previous_workday_tlref(date_val)
            
            if prev_tlref is not None:
                # Bu tarih için TLREF değerini güncelle
                anapara_float = float(anapara) if anapara else 0
                
                cur.execute("""
                    UPDATE cash_flow_analysis
                    SET tlref_faiz = %s,
                        tlref_faiz_kazanci = (%s * %s / 365.0)
                    WHERE tarih = %s
                """, (prev_tlref, prev_tlref, anapara_float, date_val))
                
                if cur.rowcount > 0:
                    updated_count += 1
                    faiz_kazanci = (prev_tlref * anapara_float) / 365.0
                    print(f"  ✓ {date_val}: TLREF %{prev_tlref:.6f} ({prev_date} tarihinden) | Kazanç: {faiz_kazanci:,.2f}")
                
            else:
                not_found_count += 1
                print(f"  ⚠ {date_val}: Önceki işgünü TLREF değeri bulunamadı")
        
        # Tüm değişiklikleri commit et
        conn.commit()
        cur.close()
        conn.close()
        
        print(f"\n=== TATİL GÜNLERİ DOLDURMA SONUCU ===")
        print(f"Güncellenene tarih: {updated_count}")
        print(f"Bulunamayan tarih: {not_found_count}")
        
    except Exception as e:
        print(f"Tatil günleri doldurma hatası: {e}")

def check_weekends_and_holidays():
    """Hafta sonları ve tatil günlerini kontrol et"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Hafta sonlarını kontrol et
        cur.execute("""
            SELECT 
                tarih,
                EXTRACT(DOW FROM tarih) as day_of_week,
                tlref_faiz,
                anapara
            FROM cash_flow_analysis 
            WHERE EXTRACT(DOW FROM tarih) IN (0, 6)  -- Pazar(0) ve Cumartesi(6)
            ORDER BY tarih
            LIMIT 10
        """)
        
        weekends = cur.fetchall()
        
        print("HAFTA SONU TARİHLERİ:")
        print("-" * 60)
        for date_val, dow, tlref, anapara in weekends:
            day_name = "Pazar" if dow == 0 else "Cumartesi"
            tlref_str = f"%{tlref:.6f}" if tlref else "NULL"
            print(f"{date_val} ({day_name}): TLREF={tlref_str} | Anapara={anapara:,.0f}")
        
        # TLREF faizi olmayan günleri kontrol et
        cur.execute("""
            SELECT COUNT(*) 
            FROM cash_flow_analysis 
            WHERE tlref_faiz IS NULL
        """)
        
        null_count = cur.fetchone()[0]
        print(f"\nToplam TLREF faizi olmayan kayıt: {null_count}")
        
        cur.close()
        conn.close()
        
    except Exception as e:
        print(f"Hafta sonu kontrol hatası: {e}")

def verify_tlref_coverage():
    """TLREF kapsamını doğrula"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Genel istatistikler
        cur.execute("""
            SELECT 
                COUNT(*) as toplam,
                COUNT(tlref_faiz) as tlref_var,
                MIN(tarih) as min_tarih,
                MAX(tarih) as max_tarih
            FROM cash_flow_analysis
        """)
        
        stats = cur.fetchone()
        toplam, tlref_var, min_tarih, max_tarih = stats
        
        print("TLREF KAPSAM RAPORU:")
        print("=" * 50)
        print(f"Toplam kayıt: {toplam}")
        print(f"TLREF faizi olan: {tlref_var}")
        print(f"TLREF faizi olmayan: {toplam - tlref_var}")
        print(f"Kapsam oranı: %{(tlref_var/toplam)*100:.1f}")
        print(f"Tarih aralığı: {min_tarih} - {max_tarih}")
        
        # En son güncellenmiş kayıtları göster
        cur.execute("""
            SELECT tarih, tlref_faiz, tlref_faiz_kazanci, anapara
            FROM cash_flow_analysis 
            WHERE tlref_faiz IS NOT NULL 
            ORDER BY tarih DESC 
            LIMIT 5
        """)
        
        recent = cur.fetchall()
        print(f"\nEn son TLREF kayıtları:")
        for date_val, tlref, kazanc, anapara in recent:
            print(f"  {date_val}: %{tlref:.6f} | Kazanç: {kazanc:,.2f} | Anapara: {anapara:,.0f}")
        
        cur.close()
        conn.close()
        
    except Exception as e:
        print(f"Doğrulama hatası: {e}")

def main():
    print("=== Tatil Günleri TLREF Doldurma ===")
    print("Eksik tarihlere önceki işgününün TLREF değeri uygulanacak")
    print()
    
    # 1. Mevcut durumu kontrol et
    print("1. Mevcut TLREF durumu kontrol ediliyor...")
    verify_tlref_coverage()
    
    # 2. Hafta sonları ve tatilleri kontrol et
    print("\n2. Hafta sonları kontrol ediliyor...")
    check_weekends_and_holidays()
    
    # 3. İşlem seçeneği
    print(f"\n3. İşlem seçenekleri:")
    print(f"1. Tatil günlerini önceki işgünü TLREF ile doldur")
    print(f"2. Sadece kontrol yap, güncelleme yapma")
    print(f"3. İptal")
    
    try:
        choice = input("\nSeçiminizi yapın (1/2/3): ")
        
        if choice == "1":
            print(f"\n4. Tatil günleri dolduruluyor...")
            fill_holiday_tlref()
            
            print(f"\n5. Güncellenmiş durum kontrol ediliyor...")
            verify_tlref_coverage()
            
        elif choice == "2":
            print("Sadece kontrol yapıldı, güncelleme yapılmadı.")
            
        else:
            print("İşlem iptal edildi.")
    
    except KeyboardInterrupt:
        print("\nİşlem kullanıcı tarafından iptal edildi.")
    
    print(f"\n=== İşlem Tamamlandı ===")

if __name__ == "__main__":
    main()