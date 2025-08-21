import pandas as pd
import psycopg2
from datetime import datetime
import os
import time

# -------------------------------
# AYARLAR
# -------------------------------
EXCEL_FILE = "EVDS_Uzun_Tarih.xlsx"
DB_CONFIG = {
    "host":"192.168.182.3","dbname":"tmks-ftp","user":"postgres","password":"postgres.db!"
}
TABLE_NAME = "TLREF"
BATCH_SIZE = 100  # Büyük veri için batch boyutu

def create_tlref_table():
    """TLREF tablosunu oluştur"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Önce tabloyu sil (varsa)
        cur.execute(f"DROP TABLE IF EXISTS {TABLE_NAME}")
        
        # Yeni tabloyu oluştur
        create_table_sql = f"""
        CREATE TABLE {TABLE_NAME} (
            id SERIAL PRIMARY KEY,
            tarih DATE NOT NULL UNIQUE,
            tlref_oran DECIMAL(10, 6) NOT NULL,
            tlref_yuzde DECIMAL(8, 6) NOT NULL,
            gun_adi VARCHAR(20),
            hafta_sonu BOOLEAN DEFAULT FALSE,
            yil INTEGER,
            ay INTEGER,
            gun INTEGER,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        """
        
        cur.execute(create_table_sql)
        
        # İndeksler oluştur
        cur.execute(f"CREATE INDEX idx_{TABLE_NAME}_tarih ON {TABLE_NAME}(tarih);")
        cur.execute(f"CREATE INDEX idx_{TABLE_NAME}_yil_ay ON {TABLE_NAME}(yil, ay);")
        cur.execute(f"CREATE INDEX idx_{TABLE_NAME}_hafta_sonu ON {TABLE_NAME}(hafta_sonu);")
        
        conn.commit()
        cur.close()
        conn.close()
        
        print(f"✓ Tablo '{TABLE_NAME}' başarıyla oluşturuldu")
        return True
        
    except Exception as e:
        print(f"✗ Tablo oluşturma hatası: {e}")
        return False

def fill_missing_dates(df):
    """Eksik tarihleri bir önceki günün TLREF değeri ile doldur"""
    try:
        # Tarih aralığını belirle
        start_date = df['Tarih'].min()
        end_date = df['Tarih'].max()
        
        print(f"Tarih aralığı: {start_date.strftime('%d.%m.%Y')} - {end_date.strftime('%d.%m.%Y')}")
        
        # Tüm tarihlerin olması gereken aralığı oluştur
        full_date_range = pd.date_range(start=start_date, end=end_date, freq='D')
        
        # Mevcut tarihleri set'e çevir
        existing_dates = set(df['Tarih'].dt.date)
        
        # Eksik tarihleri bul
        missing_dates = []
        for date in full_date_range:
            if date.date() not in existing_dates:
                missing_dates.append(date)
        
        print(f"Eksik tarih sayısı: {len(missing_dates)}")
        
        if len(missing_dates) == 0:
            print("Tüm tarihler mevcut, doldurma gerekmiyor.")
            return df
        
        # Eksik tarihleri doldur
        filled_records = []
        
        for missing_date in missing_dates:
            # Önceki günün TLREF değerini bul
            prev_date = missing_date - pd.Timedelta(days=1)
            search_attempts = 0
            prev_tlref = None
            
            # Maksimum 7 gün geriye git
            while search_attempts < 7 and prev_tlref is None:
                prev_record = df[df['Tarih'].dt.date == prev_date.date()]
                
                if not prev_record.empty:
                    prev_tlref = prev_record.iloc[0]['TLREF']
                    break
                
                prev_date = prev_date - pd.Timedelta(days=1)
                search_attempts += 1
            
            if prev_tlref is not None:
                # Yeni kayıt oluştur
                new_record = {
                    'Tarih': missing_date,
                    'TLREF': prev_tlref,
                    'TLREF_Yuzde': prev_tlref / 100.0,
                    'Gun_Adi': missing_date.day_name(),
                    'Hafta_Sonu': missing_date.dayofweek >= 5,
                    'Yil': missing_date.year,
                    'Ay': missing_date.month,
                    'Gun': missing_date.day
                }
                filled_records.append(new_record)
                
                # İlk 10 tanesini göster
                if len(filled_records) <= 10:
                    print(f"  {missing_date.strftime('%d.%m.%Y')} ({missing_date.day_name()}): TLREF {prev_tlref:.4f} (önceki günden)")
        
        if len(filled_records) > 10:
            print(f"  ... ve {len(filled_records) - 10} tarih daha dolduruldu")
        
        # Yeni kayıtları DataFrame'e ekle
        if filled_records:
            filled_df = pd.DataFrame(filled_records)
            combined_df = pd.concat([df, filled_df], ignore_index=True)
            combined_df = combined_df.sort_values('Tarih').reset_index(drop=True)
            
            print(f"Toplam kayıt: {len(df)} -> {len(combined_df)} (+{len(filled_records)} dolduruldu)")
            return combined_df
        else:
            print("Hiçbir eksik tarih doldurulamadı")
            return df
            
    except Exception as e:
        print(f"Tarih doldurma hatası: {e}")
        return df

def read_excel_long_data():
    """Excel dosyasından uzun vadeli TLREF verilerini oku"""
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
            print(f"{EXCEL_FILE} dosyası bulunamadı.")
            return None
            
        print(f"Excel dosyası okunuyor: {file_path}")
        
        # Excel dosyasını oku
        df = pd.read_excel(file_path, sheet_name='EVDS')
        
        print(f"Ham veri: {len(df)} satır")
        
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
            
        print(f"TLREF sütunu: {tlref_column}")
        
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
        
        # Tarihe göre sırala (eskiden yeniye)
        df = df.sort_values(date_column, ascending=True)
        
        # Kolon isimlerini standartlaştır
        df = df.rename(columns={
            date_column: 'Tarih',
            tlref_column: 'TLREF'
        })
        
        # Ek bilgiler ekle
        df['TLREF_Yuzde'] = df['TLREF'] / 100.0
        df['Gun_Adi'] = df['Tarih'].dt.day_name()
        df['Hafta_Sonu'] = df['Tarih'].dt.dayofweek >= 5  # Cumartesi(5) ve Pazar(6)
        df['Yil'] = df['Tarih'].dt.year
        df['Ay'] = df['Tarih'].dt.month
        df['Gun'] = df['Tarih'].dt.day
        
        print(f"İşlenmiş veri: {len(df)} satır")
        print(f"Tarih aralığı: {df['Tarih'].min().strftime('%d.%m.%Y')} - {df['Tarih'].max().strftime('%d.%m.%Y')}")
        print(f"TLREF aralığı: {df['TLREF'].min():.4f} - {df['TLREF'].max():.4f}")
        
        # Eksik tarihleri doldur
        print(f"\nEksik tarihler dolduruluyor...")
        df = fill_missing_dates(df)
        
        return df
        
    except Exception as e:
        print(f"Excel okuma hatası: {e}")
        return None

def insert_data_in_batches(df):
    """Veriyi batch'ler halinde tabloya ekle"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        total_records = len(df)
        total_batches = (total_records + BATCH_SIZE - 1) // BATCH_SIZE
        
        print(f"\n{total_records} kayıt {BATCH_SIZE}'er kayıt halinde {total_batches} batch'te ekleniyor...")
        
        inserted_count = 0
        
        for i in range(0, total_records, BATCH_SIZE):
            batch_df = df.iloc[i:i+BATCH_SIZE]
            batch_num = (i // BATCH_SIZE) + 1
            
            print(f"Batch {batch_num}/{total_batches} işleniyor...")
            
            # Batch verilerini hazırla
            insert_data = []
            for _, row in batch_df.iterrows():
                insert_data.append((
                    row['Tarih'].date(),
                    float(row['TLREF']),
                    float(row['TLREF_Yuzde']),
                    row['Gun_Adi'],
                    bool(row['Hafta_Sonu']),
                    int(row['Yil']),
                    int(row['Ay']),
                    int(row['Gun'])
                ))
            
            # Batch insert
            insert_sql = f"""
                INSERT INTO {TABLE_NAME} 
                (tarih, tlref_oran, tlref_yuzde, gun_adi, hafta_sonu, yil, ay, gun)
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                ON CONFLICT (tarih) DO UPDATE SET
                    tlref_oran = EXCLUDED.tlref_oran,
                    tlref_yuzde = EXCLUDED.tlref_yuzde,
                    gun_adi = EXCLUDED.gun_adi,
                    hafta_sonu = EXCLUDED.hafta_sonu,
                    updated_at = CURRENT_TIMESTAMP
            """
            
            cur.executemany(insert_sql, insert_data)
            conn.commit()
            
            inserted_count += len(insert_data)
            progress = (batch_num / total_batches) * 100
            print(f"  ✓ {len(insert_data)} kayıt eklendi | İlerleme: {progress:.1f}%")
            
            time.sleep(0.1)  # Kısa bekleme
        
        cur.close()
        conn.close()
        
        print(f"\n✓ Toplam {inserted_count} kayıt başarıyla tabloya eklendi")
        return True
        
    except Exception as e:
        print(f"✗ Veri ekleme hatası: {e}")
        return False

def verify_table_data():
    """Tablo verilerini doğrula"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Genel istatistikler
        cur.execute(f"""
            SELECT 
                COUNT(*) as toplam_kayit,
                MIN(tarih) as min_tarih,
                MAX(tarih) as max_tarih,
                MIN(tlref_oran) as min_tlref,
                MAX(tlref_oran) as max_tlref,
                COUNT(CASE WHEN hafta_sonu = true THEN 1 END) as hafta_sonu_sayisi
            FROM {TABLE_NAME}
        """)
        
        stats = cur.fetchone()
        toplam, min_tarih, max_tarih, min_tlref, max_tlref, hafta_sonu = stats
        
        print("\n=== TABLO DOĞRULAMA RAPORU ===")
        print(f"Tablo adı: {TABLE_NAME}")
        print(f"Toplam kayıt: {toplam:,}")
        print(f"Tarih aralığı: {min_tarih} - {max_tarih}")
        print(f"TLREF aralığı: {min_tlref:.4f} - {max_tlref:.4f}")
        print(f"Hafta sonu kayıt: {hafta_sonu}")
        
        # Yıllık istatistikler
        cur.execute(f"""
            SELECT 
                yil,
                COUNT(*) as kayit_sayisi,
                AVG(tlref_yuzde) as ortalama_tlref,
                MIN(tlref_yuzde) as min_tlref,
                MAX(tlref_yuzde) as max_tlref
            FROM {TABLE_NAME}
            GROUP BY yil
            ORDER BY yil
        """)
        
        yearly_stats = cur.fetchall()
        print(f"\nYıllık İstatistikler:")
        print("-" * 80)
        print("Yıl    | Kayıt | Ort TLREF(%) | Min TLREF(%) | Max TLREF(%)")
        print("-" * 80)
        for yil, kayit, ort, min_val, max_val in yearly_stats:
            print(f"{yil} | {kayit:5} | {ort:.6f}   | {min_val:.6f}   | {max_val:.6f}")
        
        # Son 10 kayıt
        cur.execute(f"""
            SELECT tarih, tlref_oran, tlref_yuzde, gun_adi, hafta_sonu
            FROM {TABLE_NAME}
            ORDER BY tarih DESC
            LIMIT 10
        """)
        
        recent = cur.fetchall()
        print(f"\nSon 10 kayıt:")
        for tarih, oran, yuzde, gun, hafta_sonu in recent:
            hafta_sonu_str = "HaftaSonu" if hafta_sonu else gun
            print(f"  {tarih} ({hafta_sonu_str}): {oran:.4f} (%{yuzde:.6f})")
        
        cur.close()
        conn.close()
        
    except Exception as e:
        print(f"✗ Doğrulama hatası: {e}")

def create_useful_views():
    """Faydalı view'lar oluştur"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # 1. İşgünleri view'ı
        cur.execute(f"""
            CREATE OR REPLACE VIEW {TABLE_NAME}_workdays AS
            SELECT * FROM {TABLE_NAME}
            WHERE hafta_sonu = FALSE
            ORDER BY tarih;
        """)
        
        # 2. Aylık ortalama view'ı
        cur.execute(f"""
            CREATE OR REPLACE VIEW {TABLE_NAME}_monthly_avg AS
            SELECT 
                yil,
                ay,
                COUNT(*) as gun_sayisi,
                AVG(tlref_yuzde) as ortalama_tlref,
                MIN(tlref_yuzde) as min_tlref,
                MAX(tlref_yuzde) as max_tlref,
                STDDEV(tlref_yuzde) as standart_sapma
            FROM {TABLE_NAME}
            GROUP BY yil, ay
            ORDER BY yil, ay;
        """)
        
        # 3. Yıllık trend view'ı
        cur.execute(f"""
            CREATE OR REPLACE VIEW {TABLE_NAME}_yearly_trend AS
            SELECT 
                yil,
                COUNT(*) as toplam_gun,
                AVG(tlref_yuzde) as ortalama_tlref,
                MIN(tlref_yuzde) as min_tlref,
                MAX(tlref_yuzde) as max_tlref,
                MAX(tlref_yuzde) - MIN(tlref_yuzde) as volatilite
            FROM {TABLE_NAME}
            GROUP BY yil
            ORDER BY yil;
        """)
        
        conn.commit()
        cur.close()
        conn.close()
        
        print("\n✓ Faydalı view'lar oluşturuldu:")
        print(f"  - {TABLE_NAME}_workdays (sadece işgünleri)")
        print(f"  - {TABLE_NAME}_monthly_avg (aylık ortalamalar)")
        print(f"  - {TABLE_NAME}_yearly_trend (yıllık trendler)")
        
    except Exception as e:
        print(f"✗ View oluşturma hatası: {e}")

def main():
    print("=== TLREF Tarihi Veri Tablosu Oluşturucu ===")
    print("EVDS_Uzun_Tarih.xlsx dosyasından veritabanına tablo oluşturacak")
    print()
    
    # 1. Excel dosyasını oku
    print("1. Excel dosyası okunuyor...")
    df = read_excel_long_data()
    
    if df is None:
        print("Excel dosyası okunamadı. İşlem durduruluyor.")
        return
    
    # 2. Veri özeti
    print(f"\n2. Veri özeti:")
    print(f"   Toplam kayıt: {len(df):,}")
    print(f"   Tarih aralığı: {df['Tarih'].min().strftime('%d.%m.%Y')} - {df['Tarih'].max().strftime('%d.%m.%Y')}")
    print(f"   Hafta sonu kayıt: {df['Hafta_Sonu'].sum()}")
    print(f"   İşgünü kayıt: {(~df['Hafta_Sonu']).sum()}")
    
    # 3. İşlem seçeneği
    print(f"\n3. İşlem seçenekleri:")
    print(f"1. Yeni tablo oluştur ve verileri ekle (UYARI: Mevcut TLREF tablosu silinecek!)")
    print(f"2. Mevcut tabloya yeni verileri ekle/güncelle")
    print(f"3. Sadece veri kontrolü yap")
    print(f"4. İptal")
    
    try:
        choice = input("\nSeçiminizi yapın (1/2/3/4): ")
        
        if choice == "1":
            print(f"\n⚠ UYARI: Bu işlem mevcut TLREF tablosunu silecek!")
            confirm = input("Devam etmek istediğinizden emin misiniz? (EVET/hayır): ")
            
            if confirm.upper() == "EVET":
                print(f"\n4. Veritabanı tablosu oluşturuluyor...")
                if not create_tlref_table():
                    return
                
                print(f"\n5. Veriler tabloya ekleniyor...")
                if not insert_data_in_batches(df):
                    return
                
                print(f"\n6. Tablo verileri doğrulanıyor...")
                verify_table_data()
                
                print(f"\n7. Faydalı view'lar oluşturuluyor...")
                create_useful_views()
            else:
                print("İşlem iptal edildi.")
                
        elif choice == "2":
            print(f"\n4. Mevcut tabloya veriler ekleniyor/güncelleniyor...")
            if not insert_data_in_batches(df):
                return
            
            print(f"\n5. Tablo verileri doğrulanıyor...")
            verify_table_data()
            
        elif choice == "3":
            print("Sadece veri kontrolü yapıldı.")
            
        else:
            print("İşlem iptal edildi.")
    
    except KeyboardInterrupt:
        print("\nİşlem kullanıcı tarafından iptal edildi.")
    
    print(f"\n=== İşlem Tamamlandı ===")
    print(f"Tablo adı: {TABLE_NAME}")
    print("Bu tabloyu diğer sorgularınızda kullanabilirsiniz.")

if __name__ == "__main__":
    main()