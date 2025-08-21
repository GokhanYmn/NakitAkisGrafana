import pandas as pd
import psycopg2
from datetime import datetime
import os
import time

# -------------------------------
# AYARLAR
# -------------------------------
EXCEL_FILE = "EVDS.xlsx"
CSV_FILE = "tlref_batch_data.csv"
DB_CONFIG = {
    "host":"192.168.182.3","dbname":"tmks-ftp","user":"postgres","password":"postgres.db!"
}
BATCH_SIZE = 10  # Her seferde kaç kayıt işlensin

def read_excel_tlref():
    """Excel dosyasından TLREF verilerini oku"""
    try:
        # Dosya yolu kontrolü
        desktop_path = os.path.join(os.path.expanduser("~"), "Desktop", EXCEL_FILE)
        current_path = EXCEL_FILE
        
        file_path = None
        if os.path.exists(desktop_path):
            file_path = desktop_path
        elif os.path.exists(current_path):
            file_path = current_path
        else:
            print(f"EVDS.xlsx dosyası bulunamadı.")
            return None
            
        print(f"Excel dosyası okunuyor: {file_path}")
        
        # Excel dosyasını oku
        df = pd.read_excel(file_path, sheet_name='EVDS')
        
        # Sütun isimlerini temizle
        df.columns = df.columns.str.strip()
        
        # TLREF sütununu bul
        tlref_column = None
        for col in df.columns:
            if 'TLREF' in col.upper() or 'ORAN' in col.upper():
                tlref_column = col
                break
        
        if tlref_column is None:
            print("TLREF sütunu bulunamadı")
            return None
            
        # Veriyi temizle
        df = df.dropna(subset=[tlref_column])
        
        # Tarih sütununu datetime'a çevir
        date_column = df.columns[0]
        
        try:
            if df[date_column].dtype == 'object':
                df[date_column] = pd.to_datetime(df[date_column], format='%d-%m-%Y', errors='coerce')
            else:
                df[date_column] = pd.to_datetime(df[date_column], errors='coerce')
        except:
            print("Tarih dönüşümünde sorun var")
            return None
        
        # Tarih dönüşümü başarısız olan satırları kaldır
        df = df.dropna(subset=[date_column])
        
        # Tarihe göre sırala (en yeniden en eskiye)
        df = df.sort_values(date_column, ascending=False)
        
        # Kolon isimlerini standartlaştır
        df = df.rename(columns={
            date_column: 'Tarih',
            tlref_column: 'TLREF'
        })
        
        print(f"Excel'den {len(df)} TLREF verisi okundu")
        return df
        
    except Exception as e:
        print(f"Excel okuma hatası: {e}")
        return None

def process_batch(batch_df, batch_num, total_batches):
    """Bir batch'i işle"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        print(f"\n--- Batch {batch_num}/{total_batches} İşleniyor ({len(batch_df)} kayıt) ---")
        
        updated_count = 0
        not_found_count = 0
        error_count = 0
        
        for idx, row in batch_df.iterrows():
            try:
                date_val = row['Tarih'].date()
                tlref_raw = float(row['TLREF'])
                tlref_percentage = tlref_raw / 100.0
                
                # Bu tarih için kayıt var mı kontrol et
                cur.execute("""
                    SELECT COUNT(*), anapara 
                    FROM cash_flow_analysis 
                    WHERE tarih = %s 
                    GROUP BY anapara
                """, (date_val,))
                
                result = cur.fetchone()
                
                if result:
                    count, anapara = result
                    
                    # Decimal Decimal hatası için anapara'yı float'a çevir
                    anapara = float(anapara) if anapara else 0
                    
                    # Güncelle
                    cur.execute("""
                        UPDATE cash_flow_analysis
                        SET tlref_faiz = %s,
                            tlref_faiz_kazanci = (%s * anapara / 365.0)
                        WHERE tarih = %s
                    """, (tlref_percentage, tlref_percentage, date_val))
                    
                    if cur.rowcount > 0:
                        updated_count += 1
                        faiz_kazanci = (tlref_percentage * anapara) / 365.0
                        print(f"  ✓ {date_val}: %{tlref_percentage:.6f} | Kazanç: {faiz_kazanci:,.2f}")
                    else:
                        error_count += 1
                        print(f"  ✗ {date_val}: Güncelleme başarısız")
                else:
                    not_found_count += 1
                    print(f"  ⚠ {date_val}: Veritabanında yok")
                    
            except Exception as e:
                error_count += 1
                print(f"  ✗ {date_val}: Hata - {e}")
        
        # Batch'i commit et
        conn.commit()
        cur.close()
        conn.close()
        
        print(f"Batch {batch_num} tamamlandı: ✓{updated_count} ⚠{not_found_count} ✗{error_count}")
        
        # Kısa bekleme (veritabanı rahatlaması için)
        time.sleep(1)
        
        return updated_count, not_found_count, error_count
        
    except Exception as e:
        print(f"Batch {batch_num} hatası: {e}")
        return 0, 0, 1

def update_all_in_batches(df):
    """Tüm veriyi batch'ler halinde güncelle"""
    total_records = len(df)
    total_batches = (total_records + BATCH_SIZE - 1) // BATCH_SIZE
    
    print(f"\n{total_records} kayıt {BATCH_SIZE}'er kayıt halinde {total_batches} batch'te işlenecek")
    
    total_updated = 0
    total_not_found = 0
    total_errors = 0
    
    # DataFrame'i batch'lere böl
    for i in range(0, total_records, BATCH_SIZE):
        batch_df = df.iloc[i:i+BATCH_SIZE]
        batch_num = (i // BATCH_SIZE) + 1
        
        updated, not_found, errors = process_batch(batch_df, batch_num, total_batches)
        
        total_updated += updated
        total_not_found += not_found
        total_errors += errors
        
        # İlerleme göster
        progress = (batch_num / total_batches) * 100
        print(f"İlerleme: {progress:.1f}% ({batch_num}/{total_batches})")
    
    print(f"\n=== TOPLU İŞLEM SONUCU ===")
    print(f"Toplam işlenen: {total_records}")
    print(f"Başarıyla güncellenen: {total_updated}")
    print(f"Veritabanında bulunamayan: {total_not_found}")
    print(f"Hata alan: {total_errors}")
    print(f"Başarı oranı: {(total_updated/total_records)*100:.1f}%")

def verify_updates(df):
    """Güncellemeleri doğrula"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        print(f"\n=== DOĞRULAMA ===")
        
        # TLREF'i güncellenmiş kayıtları say
        cur.execute("""
            SELECT COUNT(*) 
            FROM cash_flow_analysis 
            WHERE tlref_faiz IS NOT NULL AND tlref_faiz > 0
        """)
        
        updated_count = cur.fetchone()[0]
        print(f"TLREF faizi olan kayıt sayısı: {updated_count}")
        
        # Son 5 güncellemeyi göster
        cur.execute("""
            SELECT tarih, tlref_faiz, tlref_faiz_kazanci, anapara
            FROM cash_flow_analysis 
            WHERE tlref_faiz IS NOT NULL 
            ORDER BY tarih DESC 
            LIMIT 5
        """)
        
        recent_updates = cur.fetchall()
        print(f"\nSon 5 güncellenmiş kayıt:")
        for record in recent_updates:
            tarih, tlref_faiz, kazanc, anapara = record
            print(f"  {tarih}: TLREF %{tlref_faiz:.6f} | Anapara: {anapara:,.0f} | Kazanç: {kazanc:,.2f}")
        
        cur.close()
        conn.close()
        
    except Exception as e:
        print(f"Doğrulama hatası: {e}")

def save_to_csv(df):
    """CSV'ye kaydet"""
    try:
        df_csv = df.copy()
        df_csv['Tarih'] = df_csv['Tarih'].dt.strftime('%d.%m.%Y')
        df_csv['TLREF_Percentage'] = df_csv['TLREF'] / 100.0
        
        df_csv.to_csv(CSV_FILE, index=False, encoding='utf-8')
        print(f"Veriler CSV'ye kaydedildi: {CSV_FILE}")
        return True
    except Exception as e:
        print(f"CSV kaydetme hatası: {e}")
        return False

def main():
    print("=== Batch TLREF İşleyici ===")
    print(f"Batch boyutu: {BATCH_SIZE} kayıt")
    print()
    
    # 1. Excel dosyasını oku
    print("1. Excel dosyası okunuyor...")
    df = read_excel_tlref()
    
    if df is None:
        print("Excel dosyası okunamadı. İşlem durduruluyor.")
        return
    
    # 2. Veri özeti
    print(f"\n2. Veri özeti:")
    print(f"   Toplam kayıt: {len(df)}")
    print(f"   En eski tarih: {df['Tarih'].min().strftime('%d.%m.%Y')}")
    print(f"   En yeni tarih: {df['Tarih'].max().strftime('%d.%m.%Y')}")
    print(f"   TLREF aralığı: {df['TLREF'].min():.4f} - {df['TLREF'].max():.4f}")
    
    # 3. CSV kaydet
    print("\n3. CSV dosyasına kaydediliyor...")
    save_to_csv(df)
    
    # 4. İşlem seçeneği
    print(f"\n4. İşlem seçenekleri:")
    print(f"1. Tüm veriyi batch'ler halinde güncelle ({len(df)} kayıt)")
    print(f"2. Sadece CSV kaydet, veritabanı güncelleme yapma")
    print(f"3. İptal")
    
    try:
        choice = input("\nSeçiminizi yapın (1/2/3): ")
        
        if choice == "1":
            print(f"\n5. Batch güncelleme başlıyor...")
            update_all_in_batches(df)
            
            print(f"\n6. Doğrulama yapılıyor...")
            verify_updates(df)
            
        elif choice == "2":
            print("Sadece CSV kaydedildi, veritabanı güncellenmedi.")
            
        else:
            print("İşlem iptal edildi.")
    
    except KeyboardInterrupt:
        print("\nİşlem kullanıcı tarafından iptal edildi.")
    
    print(f"\n=== İşlem Tamamlandı ===")

if __name__ == "__main__":
    main()