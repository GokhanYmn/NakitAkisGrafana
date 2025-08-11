import React, { useState } from 'react';

interface AnalysisChartProps {
  kaynakKurulus: string;
  fonNo: string;
  ihracNo: string;
}

const AnalysisChart: React.FC<AnalysisChartProps> = ({ kaynakKurulus, fonNo, ihracNo }) => {
  const [faizOrani, setFaizOrani] = useState<number>(15);
  const [analysisData, setAnalysisData] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  const handleAnalysisCalculate = async () => {
    setLoading(true);
    try {
      // Simüle edilmiş analiz verisi - sonra gerçek API'yi çağıracağız
      const mockData = {
        toplamFaizTutari: 2500000,
        toplamModelFaizTutari: 2750000,
        farkTutari: -250000,
        farkYuzdesi: -9.09,
        faizOrani: faizOrani
      };
      
      // Simüle gecikme
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      setAnalysisData(mockData);
    } catch (error) {
      console.error('Analysis calculation failed:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ 
      padding: '20px', 
      border: '1px solid #ddd', 
      borderRadius: '8px',
      backgroundColor: 'white'
    }}>
      <h3>💰 Nakit Akış Analizi</h3>
      
      {/* Faiz Oranı Input */}
      <div style={{ marginBottom: '20px', padding: '15px', backgroundColor: '#f8f9fa', borderRadius: '6px' }}>
        <label style={{ display: 'block', marginBottom: '10px', fontWeight: 'bold' }}>
          📊 Faiz Oranı (%):
        </label>
        <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
          <input 
            type="number" 
            value={faizOrani}
            onChange={(e) => setFaizOrani(Number(e.target.value))}
            min="0"
            max="100"
            step="0.1"
            style={{ 
              padding: '8px 12px', 
              fontSize: '16px', 
              width: '120px',
              borderRadius: '4px',
              border: '1px solid #ddd'
            }}
          />
          <button 
            onClick={handleAnalysisCalculate}
            disabled={loading}
            style={{ 
              padding: '10px 20px', 
              backgroundColor: '#007bff', 
              color: 'white', 
              border: 'none', 
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '14px'
            }}
          >
            {loading ? '⏳ Hesaplanıyor...' : '🔄 Analiz Et'}
          </button>
        </div>
      </div>

      {/* Aktif Filtreler */}
      <div style={{ 
        marginBottom: '20px', 
        padding: '10px', 
        backgroundColor: '#e8f5e8', 
        borderRadius: '6px',
        fontSize: '14px'
      }}>
        <strong>🎯 Analiz Parametreleri:</strong><br />
        🏢 Kuruluş: <strong>{kaynakKurulus}</strong><br />
        {fonNo && <>💼 Fon: <strong>{fonNo}</strong><br /></>}
        {ihracNo && <>🎯 İhraç: <strong>{ihracNo}</strong><br /></>}
        📈 Faiz Oranı: <strong>{faizOrani}%</strong>
      </div>

      {/* Loading State */}
      {loading && (
        <div style={{ 
          padding: '40px', 
          textAlign: 'center',
          backgroundColor: '#fff3cd',
          borderRadius: '6px'
        }}>
          <h4>⏳ Nakit Akış Analizi Hesaplanıyor...</h4>
          <p>Faiz oranı: <strong>{faizOrani}%</strong></p>
        </div>
      )}

      {/* Analysis Results */}
      {!loading && analysisData && (
        <div>
          <h4>📊 Analiz Sonuçları</h4>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
            gap: '15px',
            marginTop: '15px'
          }}>
            {/* Toplam Faiz Tutarı */}
            <div style={{ 
              padding: '20px', 
              backgroundColor: '#e8f5e8', 
              borderRadius: '8px',
              textAlign: 'center'
            }}>
              <h4 style={{ margin: '0 0 10px 0', color: '#28a745' }}>💚 Gerçek Faiz Tutarı</h4>
              <p style={{ 
                fontSize: '24px', 
                fontWeight: 'bold', 
                color: '#28a745',
                margin: '0'
              }}>
                ₺{analysisData.toplamFaizTutari.toLocaleString('tr-TR')}
              </p>
            </div>

            {/* Model Faiz Tutarı */}
            <div style={{ 
              padding: '20px', 
              backgroundColor: '#e3f2fd', 
              borderRadius: '8px',
              textAlign: 'center'
            }}>
              <h4 style={{ margin: '0 0 10px 0', color: '#007bff' }}>🎯 Model Faiz Tutarı</h4>
              <p style={{ 
                fontSize: '24px', 
                fontWeight: 'bold', 
                color: '#007bff',
                margin: '0'
              }}>
                ₺{analysisData.toplamModelFaizTutari.toLocaleString('tr-TR')}
              </p>
              <small style={{ color: '#666' }}>({faizOrani}% oranında)</small>
            </div>

            {/* Fark */}
            <div style={{ 
              padding: '20px', 
              backgroundColor: analysisData.farkTutari >= 0 ? '#e8f5e8' : '#f8d7da', 
              borderRadius: '8px',
              textAlign: 'center'
            }}>
              <h4 style={{ 
                margin: '0 0 10px 0', 
                color: analysisData.farkTutari >= 0 ? '#28a745' : '#dc3545' 
              }}>
                {analysisData.farkTutari >= 0 ? '📈' : '📉'} Fark
              </h4>
              <p style={{ 
                fontSize: '24px', 
                fontWeight: 'bold', 
                color: analysisData.farkTutari >= 0 ? '#28a745' : '#dc3545',
                margin: '0'
              }}>
                ₺{analysisData.farkTutari.toLocaleString('tr-TR')}
              </p>
              <small style={{ 
                color: analysisData.farkTutari >= 0 ? '#28a745' : '#dc3545',
                fontWeight: 'bold'
              }}>
                ({analysisData.farkYuzdesi > 0 ? '+' : ''}{analysisData.farkYuzdesi}%)
              </small>
            </div>
          </div>

          {/* Analiz Özeti */}
          <div style={{ 
            marginTop: '20px', 
            padding: '15px', 
            backgroundColor: '#f8f9fa', 
            borderRadius: '6px'
          }}>
            <h4>📋 Analiz Özeti</h4>
            <ul style={{ margin: '0', paddingLeft: '20px' }}>
              <li>
                <strong>Faiz Performansı:</strong> {
                  analysisData.farkTutari >= 0 
                    ? '✅ Model faiz oranının üzerinde performans' 
                    : '⚠️ Model faiz oranının altında performans'
                }
              </li>
              <li>
                <strong>Fark Oranı:</strong> {Math.abs(analysisData.farkYuzdesi)}% {
                  analysisData.farkTutari >= 0 ? 'fazla' : 'eksik'
                }
              </li>
              <li>
                <strong>Önerilen Aksiyon:</strong> {
                  analysisData.farkTutari >= 0 
                    ? '🎯 Mevcut stratejiye devam edilebilir'
                    : '🔍 Faiz stratejisi gözden geçirilmeli'
                }
              </li>
            </ul>
          </div>
        </div>
      )}

      {/* İlk Durum */}
      {!loading && !analysisData && (
        <div style={{ 
          padding: '40px', 
          textAlign: 'center',
          backgroundColor: '#f8f9fa',
          borderRadius: '6px',
          color: '#666'
        }}>
          <h4>💡 Nakit Akış Analizi</h4>
          <p>Faiz oranını belirleyip <strong>"Analiz Et"</strong> butonuna tıklayın.</p>
        </div>
      )}
    </div>
  );
};

export default AnalysisChart;