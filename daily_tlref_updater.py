import pandas as pd
import psycopg2
import requests
from datetime import datetime, timedelta
import schedule
import time
import os
import logging

# -------------------------------
# AYARLAR
# -------------------------------
DB_CONFIG = {
    "host":"192.168.182.3","dbname":"tmks-ftp","user":"postgres","password":"postgres.db!"
}
TABLE_NAME = "TLREF"
API_KEY = "xuG8dyK7UA"  # EVDS API anahtarı

# Log ayarları
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

def check_missing_dates():
    """Son 10 gün içinde eksik olan tarihleri kontrol et"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Son 10 günün tarihlerini kontrol et
        end_date = datetime.today().date()
        start_date = end_date - timedelta(days=10)
        
        # Bu aralıkta eksik olan tarihleri bul
        cur.execute(f"""
            SELECT generate_series(%s::date, %s::date, '1 day'::interval)::date as tarih
            EXCEPT
            SELECT tarih FROM {TABLE_NAME}
            WHERE tarih BETWEEN %s AND %s
            ORDER BY tarih
        """, (start_date, end_date, start_date, end_date))
        
        missing_dates = [row[0] for row in cur.fetchall()]
        
        cur.close()
        conn.close()
        
        logger.info(f"Son 10 günde {len(missing_dates)} eksik tarih bulundu")
        return missing_dates
        
    except Exception as e:
        logger.error(f"Eksik tarih kontrolü hatası: {e}")
        return []

def get_tlref_from_api(date_val):
    """EVDS API'sinden belirli tarih için TLREF değeri al"""
    try:
        date_str = date_val.strftime("%d-%m-%Y")
        
        # Birkaç farklı series kodunu dene
        series_codes = [
            "TP.BISTTLREF.ORAN",
            "TP.TLREF.AO",
            "TP.BIST.TLREF"
        ]
        
        for series_code in series_codes:
            url = f"https://evds2.tcmb.gov.tr/service/evds/series={series_code}&startDate={date_str}&endDate={date_str}&type=json"
            headers = {"key": API_KEY}
            
            try:
                response = requests.get(url, headers=headers, timeout=30)
                
                if response.status_code == 200:
                    json_data = response.json()
                    
                    if "items" in json_data and len(json_data["items"]) > 0:
                        item = json_data["items"][0]
                        series_key = series_code.replace(".", "_")
                        
                        if series_key in item and item[series_key]:
                            value = item[series_key]
                            
                            if isinstance(value, (int, float)):
                                tlref_value = float(value)
                                logger.info(f"API'den {date_str} için TLREF alındı: {tlref_value}")
                                return tlref_value
                
            except Exception as e:
                logger.warning(f"{series_code} ile {date_str} alınamadı: {e}")
                continue
        
        logger.warning(f"API'den {date_str} için TLREF alınamadı")
        return None
        
    except Exception as e:
        logger.error(f"API TLREF alma hatası: {e}")
        return None

def get_previous_tlref(target_date):
    """Önceki işgününün TLREF değerini al"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # Önceki 7 gün içinde en son TLREF değerini bul
        search_date = target_date - timedelta(days=1)
        
        for i in range(7):
            cur.execute(f"""
                SELECT tlref_oran, tlref_yuzde 
                FROM {TABLE_NAME} 
                WHERE tarih = %s
            """, (search_date,))
            
            result = cur.fetchone()
            if result:
                tlref_oran, tlref_yuzde = result
                cur.close()
                conn.close()
                logger.info(f"Önceki TLREF ({search_date}): {float(tlref_oran)}")
                return float(tlref_oran)
            
            search_date = search_date - timedelta(days=1)
        
        cur.close()
        conn.close()
        logger.warning(f"Önceki TLREF değeri bulunamadı")
        return None
        
    except Exception as e:
        logger.error(f"Önceki TLREF alma hatası: {e}")
        return None

def insert_tlref_record(date_val, tlref_oran, source="API"):
    """TLREF kaydını tabloya ekle"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        tlref_yuzde = tlref_oran / 100.0
        gun_adi = date_val.strftime('%A')
        hafta_sonu = date_val.weekday() >= 5
        
        # Türkçe gün adları
        gun_adi_tr = {
            'Monday': 'Pazartesi',
            'Tuesday': 'Salı', 
            'Wednesday': 'Çarşamba',
            'Thursday': 'Perşembe',
            'Friday': 'Cuma',
            'Saturday': 'Cumartesi',
            'Sunday': 'Pazar'
        }.get(gun_adi, gun_adi)
        
        cur.execute(f"""
            INSERT INTO {TABLE_NAME} 
            (tarih, tlref_oran, tlref_yuzde, gun_adi, hafta_sonu, yil, ay, gun)
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
            ON CONFLICT (tarih) DO UPDATE SET
                tlref_oran = EXCLUDED.tlref_oran,
                tlref_yuzde = EXCLUDED.tlref_yuzde,
                gun_adi = EXCLUDED.gun_adi,
                hafta_sonu = EXCLUDED.hafta_sonu,
                updated_at = CURRENT_TIMESTAMP
        """, (
            date_val,
            tlref_oran,
            tlref_yuzde,
            gun_adi_tr,
            hafta_sonu,
            date_val.year,
            date_val.month,
            date_val.day
        ))
        
        conn.commit()
        cur.close()
        conn.close()
        
        logger.info(f"✓ {date_val} TLREF kaydedildi: {tlref_oran:.4f}% ({source})")
        return True
        
    except Exception as e:
        logger.error(f"TLREF kaydetme hatası: {e}")
        return False

def daily_tlref_update():
    """Günlük TLREF güncelleme işlemi"""
    logger.info("=== Günlük TLREF Güncelleme Başladı ===")
    
    # 1. Eksik tarihleri kontrol et
    missing_dates = check_missing_dates()
    
    if not missing_dates:
        logger.info("Tüm tarihler güncel")
        return
    
    updated_count = 0
    
    # 2. Her eksik tarih için veri almaya çalış
    for date_val in missing_dates:
        logger.info(f"İşleniyor: {date_val}")
        
        # Önce API'den almaya çalış
        tlref_value = get_tlref_from_api(date_val)
        source = "API"
        
        # API'den alamazsa önceki günün değerini kullan
        if tlref_value is None:
            tlref_value = get_previous_tlref(date_val)
            source = "Önceki Gün"
        
        # Değer bulunduysa kaydet
        if tlref_value is not None:
            if insert_tlref_record(date_val, tlref_value, source):
                updated_count += 1
        else:
            logger.warning(f"⚠ {date_val} için TLREF değeri bulunamadı")
    
    logger.info(f"=== Güncelleme Tamamlandı: {updated_count} kayıt ===")

def update_cash_flow_tlref():
    """cash_flow_analysis tablosundaki TLREF değerlerini güncelle"""
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        # TLREF faizi olmayan kayıtları güncelle
        cur.execute(f"""
            UPDATE cash_flow_analysis cfa
            SET tlref_faiz = t.tlref_yuzde,
                tlref_faiz_kazanci = (t.tlref_yuzde * cfa.anapara / 365.0)
            FROM {TABLE_NAME} t
            WHERE cfa.tarih = t.tarih 
            AND cfa.tlref_faiz IS NULL
        """)
        
        updated_rows = cur.rowcount
        conn.commit()
        cur.close()
        conn.close()
        
        if updated_rows > 0:
            logger.info(f"cash_flow_analysis tablosunda {updated_rows} kayıt güncellendi")
        
    except Exception as e:
        logger.error(f"Cash flow TLREF güncelleme hatası: {e}")

def manual_update():
    """Manuel güncelleme"""
    print("=== Manuel TLREF Güncelleme ===")
    
    # Son durum raporu
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cur = conn.cursor()
        
        cur.execute(f"SELECT COUNT(*), MAX(tarih) FROM {TABLE_NAME}")
        count, last_date = cur.fetchone()
        
        print(f"Mevcut kayıt sayısı: {count:,}")
        print(f"Son tarih: {last_date}")
        
        cur.close()
        conn.close()
        
    except Exception as e:
        print(f"Durum kontrol hatası: {e}")
    
    # Güncelleme yap
    daily_tlref_update()
    
    # Cash flow tablosunu da güncelle
    update_cash_flow_tlref()

def setup_scheduler():
    """Otomatik zamanlanmış görevleri ayarla"""
    # Her gün saat 18:00'da çalıştır
    schedule.every().day.at("18:00").do(daily_tlref_update)
    schedule.every().day.at("18:05").do(update_cash_flow_tlref)
    
    print("=== TLREF Otomatik Güncelleme Servisi ===")
    print("Günlük güncelleme saatleri:")
    print("- 18:00: TLREF verileri güncelleme")
    print("- 18:05: Cash flow tablosu güncelleme")
    print("Servis çalışıyor... (Ctrl+C ile durdurun)")
    
    try:
        while True:
            schedule.run_pending()
            time.sleep(60)  # Her dakika kontrol et
    except KeyboardInterrupt:
        print("\nServis durduruldu.")

def main():
    print("=== Günlük TLREF Güncelleme Sistemi ===")
    print("1. Manuel güncelleme yap")
    print("2. Otomatik servis başlat (günlük 18:00)")
    print("3. İptal")
    
    try:
        choice = input("\nSeçiminizi yapın (1/2/3): ")
        
        if choice == "1":
            manual_update()
            
        elif choice == "2":
            setup_scheduler()
            
        else:
            print("İşlem iptal edildi.")
    
    except KeyboardInterrupt:
        print("\nİşlem iptal edildi.")

if __name__ == "__main__":
    main()