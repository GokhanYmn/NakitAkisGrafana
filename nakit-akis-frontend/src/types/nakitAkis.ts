
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
export interface CashFlowAnalysisData {
  timestamp: number;
  period: string;
  total_anapara: number;
  total_basit_faiz: number;
  total_faiz_kazanci: number;
  avg_basit_faiz: number; 
  total_model_faiz: number;
  total_model_faiz_kazanci: number;
  avg_model_nema_orani: number;
  total_tlref_faiz: number;
  total_tlref_kazanci: number;
  avg_tlref_faiz: number;
  basit_faiz_yield_percentage: number; 
  model_faiz_yield_percentage: number; 
  tlref_faiz_yield_percentage: number; 
  basit_vs_model_performance: number;
  basit_vs_tlref_performance: number;
  record_count: number;
  period_type: string;
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
    id: 'cash-flow-analysis', 
    name: 'Cash Flow Analizi',
    description: 'Basit, Model ve TLREF faiz karşılaştırması',
    icon: '💹'
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