import React, { useState, useEffect } from 'react';
import TrendChart from '../Charts/TrendChart';
import AnalysisChart from '../Charts/AnalysisChart';
import NakitAkisApi from '../../services/nakitAkisApi';
import { TrendData } from '../../types/nakitAkis';

interface DashboardContainerProps {
  dashboardType: string;
  kaynakKurulus: string;
  fonNo: string;
  ihracNo: string;
}

const DashboardContainer: React.FC<DashboardContainerProps> = ({
  dashboardType,
  kaynakKurulus,
  fonNo,
  ihracNo
}) => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');

  // Data yükleme
  useEffect(() => {
    if (!kaynakKurulus) return;

    const loadData = async () => {
      try {
        setLoading(true);
        setError('');
        setData(null);
        
        console.log('Dashboard data yükleniyor:', { dashboardType, kaynakKurulus, fonNo, ihracNo });

        switch (dashboardType) {
          case 'trends':
            const trendsData = await NakitAkisApi.getTrends(kaynakKurulus, fonNo, ihracNo);
            setData(trendsData);
            break;
            
          case 'analysis':
            setData({ ready: true });
            break;
            
          case 'historical':
            const historicalData = await NakitAkisApi.getHistoricalData({
              kaynakKurulus,
              fonNo,
              ihracNo
            });
            setData(historicalData);
            break;
            
          case 'comparison':
            // Karşılaştırma için birden fazla kuruluş verisi
            const comparisonData = await NakitAkisApi.getTrends(kaynakKurulus, fonNo, ihracNo);
            setData(comparisonData);
            break;
            
          default:
            setData(null);
        }
      } catch (error) {
        console.error('Dashboard data yükleme hatası:', error);
        setError(error instanceof Error ? error.message : 'Veri yükleme hatası');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [dashboardType, kaynakKurulus, fonNo, ihracNo]);

  // Loading state
  if (loading) {
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center',
        border: '1px solid #ddd',
        borderRadius: '8px',
        backgroundColor: '#fff3cd'
      }}>
        <h3>⏳ Dashboard Yükleniyor...</h3>
        <p>Tür: <strong>{dashboardType}</strong></p>
        <p>Kuruluş: <strong>{kaynakKurulus}</strong></p>
        {fonNo && <p>Fon: <strong>{fonNo}</strong></p>}
        {ihracNo && <p>İhraç: <strong>{ihracNo}</strong></p>}
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div style={{ 
        padding: '20px', 
        textAlign: 'center',
        border: '1px solid #dc3545',
        borderRadius: '8px',
        backgroundColor: '#f8d7da',
        color: '#721c24'
      }}>
        <h3>❌ Dashboard Yükleme Hatası</h3>
        <p>{error}</p>
        <button 
          onClick={() => window.location.reload()}
          style={{ 
            padding: '8px 16px', 
            backgroundColor: '#dc3545', 
            color: 'white', 
            border: 'none', 
            borderRadius: '4px' 
          }}
        >
          🔄 Tekrar Dene
        </button>
      </div>
    );
  }

  // No data state
  if (!data || (Array.isArray(data) && data.length === 0)) {
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center',
        border: '2px dashed #ddd',
        borderRadius: '8px',
        backgroundColor: '#f9f9f9'
      }}>
        <h3>📊 Veri Bulunamadı</h3>
        <p>Seçili filtreler için görüntülenecek veri yok.</p>
        <div style={{ fontSize: '14px', color: '#666', marginTop: '10px' }}>
          <strong>Aktif Filtreler:</strong><br />
          Dashboard: {dashboardType}<br />
          Kuruluş: {kaynakKurulus}<br />
          {fonNo && <>Fon: {fonNo}<br /></>}
          {ihracNo && <>İhraç: {ihracNo}<br /></>}
        </div>
      </div>
    );
  }

  // Render dashboard based on type
  const renderDashboard = () => {
    switch (dashboardType) {
      case 'trends':
        return <TrendChart data={data} kurulus={kaynakKurulus} />;
        
      case 'analysis':
  return (
    <div style={{ 
      padding: '20px', 
      border: '1px solid #ddd', 
      borderRadius: '8px',
      backgroundColor: 'white'
    }}>
      <h3>💰 Nakit Akış Analizi</h3>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '20px' }}>
        <div style={{ padding: '15px', backgroundColor: '#e8f5e8', borderRadius: '6px' }}>
          <h4>📈 Toplam Faiz Tutarı</h4>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: 'green' }}>
            ₺{data.toplamFaizTutari?.toLocaleString('tr-TR') || '0'}
          </p>
        </div>
        <div style={{ padding: '15px', backgroundColor: '#e3f2fd', borderRadius: '6px' }}>
          <h4>🎯 Model Faiz Tutarı</h4>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: 'blue' }}>
            ₺{data.toplamModelFaizTutari?.toLocaleString('tr-TR') || '0'}
          </p>
        </div>
      </div>
      <p style={{ marginTop: '15px', fontSize: '14px', color: '#666' }}>
        Son güncelleme: {new Date().toLocaleString('tr-TR')}
      </p>
    </div>
  );
        
      case 'historical':
        return (
          <div style={{ 
            padding: '20px', 
            border: '1px solid #ddd', 
            borderRadius: '8px',
            backgroundColor: 'white'
          }}>
            <h3>📊 Geçmiş Veriler</h3>
            <p>Geçmiş veri analizi burada görünecek...</p>
            <div style={{ fontSize: '14px', color: '#666' }}>
              Veri sayısı: {Array.isArray(data) ? data.length : 'N/A'}
            </div>
          </div>
        );
        
      case 'comparison':
        return (
          <div style={{ 
            padding: '20px', 
            border: '1px solid #ddd', 
            borderRadius: '8px',
            backgroundColor: 'white'
          }}>
            <h3>⚖️ Kuruluş Karşılaştırma</h3>
            <TrendChart data={data} kurulus={`${kaynakKurulus} - Karşılaştırma`} />
          </div>
        );
        
      default:
        return (
          <div style={{ padding: '20px', textAlign: 'center' }}>
            <h3>❓ Bilinmeyen Dashboard Türü</h3>
            <p>Dashboard türü: {dashboardType}</p>
          </div>
        );
    }
  };

  return (
    <div style={{ marginTop: '20px' }}>
      {renderDashboard()}
    </div>
  );
};

export default DashboardContainer;