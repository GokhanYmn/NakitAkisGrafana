
import axios from 'axios';
import { NakitAkisSonuc,GrafanaVariable,TrendData } from '../types/nakitAkis';

//Base Url - Backend API adresi
const API_BASE_URL='http://localhost:7289/api';

//Axios instance oluşturma
const api =axios.create({
    baseURL:API_BASE_URL,
    headers:{
        'Content-Type':'application/json',
    },
});

//Apı Service Class
export class NakitAkisApi{
    //Health check
    static async healthCheck():Promise<{status:string;timestamp:string}>{
        const response=await api.get('/grafana/health');
        return response.data;
    }

    //Kaynak kuruluşları getir
     static async getKaynakKuruluslar(): Promise<GrafanaVariable[]> {
    const response = await api.post('/grafana/variable', {
      variable: 'kaynak_kurulus'
    });
    return response.data;
  }

  //Trend Verileri getir
    static async getTrends(kaynakKurulus:string):Promise<TrendData[]>{
        const response=await api.get('/grafana/trends',{
            params:{
                kaynak_kurulus:kaynakKurulus,
                period:'week'
            }
        });
        return response.data;
    }
}
export default NakitAkisApi;