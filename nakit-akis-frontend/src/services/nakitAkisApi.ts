import axios from 'axios';
import { 
  NakitAkisSonuc, 
  GrafanaVariable, 
  TrendData,
  FonBilgi,
  IhracBilgi,
  AnalysisData,
  HistoricalData,
  NakitAkisParametre,
  CashFlowAnalysisData 
} from '../types/nakitAkis';

// Base URL - Backend API adresin
const API_BASE_URL = 'http://localhost:7289/api';

// Axios instance oluştur
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// API service class
export class NakitAkisApi {
  // Health check
  static async healthCheck(): Promise<{ status: string; timestamp: string }> {
    const response = await api.get('/grafana/health');
    return response.data;
  }

  // Kaynak kuruluşları getir
  static async getKaynakKuruluslar(): Promise<GrafanaVariable[]> {
    const response = await api.post('/grafana/variable', {
      variable: 'kaynak_kurulus'
    });
    return response.data;
  }

  // Fonları getir
  static async getFonlar(kaynakKurulus: string): Promise<GrafanaVariable[]> {
    const response = await api.post('/grafana/variable', {
      variable: 'fm_fonlar',
      params: { kaynak_kurulus: kaynakKurulus }
    });
    return response.data;
  }

  // İhraçları getir
  static async getIhraclar(kaynakKurulus: string, fonNo: string): Promise<GrafanaVariable[]> {
    const response = await api.post('/grafana/variable', {
      variable: 'ihrac_no',
      params: { 
        kaynak_kurulus: kaynakKurulus,
        fm_fonlar: fonNo 
      }
    });
    return response.data;
  }

  // Trend verileri getir
static async getTrends(kaynakKurulus: string, fonNo?: string, ihracNo?: string): Promise<TrendData[]> {
  try {
    console.log('API Call - getTrends:', { kaynakKurulus, fonNo, ihracNo });
    
    const params: any = {
      kaynak_kurulus: kaynakKurulus,
      period: 'week'
    };
    
    // Boş string'leri gönderme
    if (fonNo && fonNo !== '') {
      params.fm_fonlar = fonNo;
    }
    
    if (ihracNo && ihracNo !== '') {
      params.ihrac_no = ihracNo;
    }
    
    console.log('API Parameters:', params);
    console.log('Full API URL will be:', `${API_BASE_URL}/grafana/trends`);
    
    const response = await api.get('/grafana/trends', { params });
    
    console.log('API Response:', response.data);
    return response.data;
  } catch (error: any) {
    console.error('=== FULL ERROR DEBUG ===');
    console.error('getTrends API error:', error);
    console.error('Error message:', error.message);
    
    if (error.response) {
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);
      console.error('Response headers:', error.response.headers);
    } else if (error.request) {
      console.error('Request was made but no response:', error.request);
    } else {
      console.error('Error setting up request:', error.message);
    }
    
    throw error;
  }
}

  // Nakit akış analizi
  static async getAnalysis(parametreler: {
  faizOrani: number;
  kaynakKurulus: string;
  fonNo?: string;
  ihracNo?: string;
  baslangicTarihi?: string;
  bitisTarihi?: string;
}): Promise<any> {
  try {
    console.log('API Service - getAnalysis:', parametreler);
    
    const params: any = {
      faizOrani: parametreler.faizOrani,
      kaynak_kurulus: parametreler.kaynakKurulus
    };
    
    // Opsiyonel parametreler
    if (parametreler.fonNo && parametreler.fonNo !== '') {
      params.fm_fonlar = parametreler.fonNo;
    }
    
    if (parametreler.ihracNo && parametreler.ihracNo !== '') {
      params.ihrac_no = parametreler.ihracNo;
    }
    
    if (parametreler.baslangicTarihi) {
      params.baslangic_tarihi = parametreler.baslangicTarihi;
    }
    
    if (parametreler.bitisTarihi) {
      params.bitis_tarihi = parametreler.bitisTarihi;
    }
    
    console.log('API Parameters:', params);
    
    const response = await api.get('/grafana/analysis', { params });
    
    console.log('API Response:', response.data);
    return response.data;
  } catch (error: any) {
    console.error('getAnalysis API error:', error);
    if (error.response) {
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);
    }
    throw error;
  }

  }

static async getCashFlowAnalysis(period: string = 'month', limit: number = 100): Promise<CashFlowAnalysisData[]> {
  try {
    console.log('API Call - getCashFlowAnalysis:', { period, limit });
    
    const params = {
      period: period,
      limit: limit.toString()
    };
    
    const response = await api.get('/grafana/cash-flow-analysis', { params });
    
    console.log('Cash Flow Analysis Response:', response.data);
    return response.data;
  } catch (error: any) {
    console.error('getCashFlowAnalysis API error:', error);
    if (error.response) {
      console.error('Response status:', error.response.status);
      console.error('Response data:', error.response.data);
    }
    throw error;
  }
}
  // Geçmiş veriler
  static async getHistoricalData(parametreler: {
    kaynakKurulus: string;
    fonNo?: string;
    ihracNo?: string;
    baslangicTarihi?: string;
    bitisTarihi?: string;
  }): Promise<any[]> {
    const response = await api.get('/nakitakis/historical', {
      params: {
        kaynakKurulus: parametreler.kaynakKurulus,
        fonNo: parametreler.fonNo,
        ihracNo: parametreler.ihracNo,
        baslangicTarihi: parametreler.baslangicTarihi,
        bitisTarihi: parametreler.bitisTarihi
      }
    });
    return response.data;
  }

  // Bankalar listesi
  static async getBankalar(): Promise<GrafanaVariable[]> {
    const response = await api.get('/nakitakis/bankalar');
    return response.data.map((item: any) => ({
      text: `${item.bankaAdi} (${item.kayitSayisi} kayıt)`,
      value: item.bankaAdi
    }));
  }
}

export default NakitAkisApi;