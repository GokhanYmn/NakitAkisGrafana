
export interface NakitAkisParametre{
    faizOrani:number;
    kaynakKurulus:string;
    secilenBankalar:string[];
    baslangicTarihi?:Date;
    bitisTarihi?:Date;
}

export interface NakitAkisSonuc{
    toplamFaizTutari:number;
    toplamModelFaizTutari:number;
    farkTutari:number;
    farkYuzdesi:number;
}

export interface GrafanaVariable{
    text:string;
    value:string;
}

export interface TrendData{
    timestamp:number;
    hafta:string;
    fon_no:string;
    kumulatif_mevduat:number;
    kumulatif_faiz_kazanci:number;
    kumulatif_buyume_yuzde:number;
    kurulus:string;
}
// Mevcut interface'lerin altına ekle:

export interface DashboardConfig {
  id: string;
  name: string;
  description: string;
  icon: string;
}

export interface FonBilgi {
  fonNo: string;
  toplamTutar: number;
  kayitSayisi: number;
}

export interface IhracBilgi {
  ihracNo: string;
  toplamTutar: number;
  kayitSayisi: number;
}

export interface AnalysisData {
  toplamFaizTutari: number;
  toplamModelFaizTutari: number;
  farkTutari: number;
  farkYuzdesi: number;
  faizOrani: number;
}

export interface HistoricalData {
  tarih: string;
  toplamFaizTutari: number;
  toplamModelFaizTutari: number;
  farkTutari: number;
  farkYuzdesi: number;
}

// Dashboard türleri
export const DASHBOARD_TYPES: DashboardConfig[] = [
  {
    id: 'trends',
    name: 'Haftalık Trend Analizi',
    description: 'Kümülatif büyüme ve trend grafiği',
    icon: '📈'
  },
  {
    id: 'analysis',
    name: 'Nakit Akış Analizi',
    description: 'Faiz oranı analizi ve karşılaştırma',
    icon: '💰'
  },
  {
    id: 'historical',
    name: 'Geçmiş Veriler',
    description: 'Tarihsel performans analizi',
    icon: '📊'
  },
  {
    id: 'comparison',
    name: 'Kuruluş Karşılaştırma',
    description: 'Farklı kuruluşları karşılaştır',
    icon: '⚖️'
  }
];