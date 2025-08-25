import React, { useState, useEffect } from 'react';
import NakitAkisApi from '../../services/nakitAkisApi';
import { useTheme } from '../../contexts/ThemeContext'; 

interface AnalysisChartProps {
  kaynakKurulus: string;
  fonNo: string;
  ihracNo: string;
  initialFaizOrani: number;
}

const AnalysisChart: React.FC<AnalysisChartProps> = ({ 
  kaynakKurulus, 
  fonNo, 
  ihracNo, 
  initialFaizOrani 
}) => {
  const { theme } = useTheme(); 
  
  const [faizOrani, setFaizOrani] = useState<number>(initialFaizOrani);
  const [analysisData, setAnalysisData] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  // Master faiz oranı değiştiğinde sync et
  useEffect(() => {
    setFaizOrani(initialFaizOrani);
    setAnalysisData(null);
  }, [initialFaizOrani]);

  const handleAnalysisCalculate = async () => {
    setLoading(true);
    try {
      console.log('Real API call starting...', { 
        faizOrani, 
        kaynakKurulus, 
        fonNo, 
        ihracNo 
      });

      const realData = await NakitAkisApi.getAnalysis({
        faizOrani: faizOrani,
        kaynakKurulus: kaynakKurulus,
        fonNo: fonNo,
        ihracNo: ihracNo
      });

      console.log('Real API response:', realData);
      setAnalysisData(realData);
    } catch (error: any) {
      console.error('Analysis API call failed:', error);
      
      setAnalysisData({
        toplamFaizTutari: 0,
        toplamModelFaizTutari: 0,
        farkTutari: 0,
        farkYuzdesi: 0,
        faizOrani: faizOrani,
        mesaj: 'API hatası: ' + (error.message || 'Bilinmeyen hata')
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ 
      padding: '20px', 
      border: `1px solid ${theme.colors.border}`, // TEMA BORDER
      borderRadius: '8px',
      backgroundColor: theme.colors.surface // TEMA BACKGROUND
    }}>
      <h3 style={{ color: theme.colors.text }}>💰 Nakit Akış Analizi</h3>
      
      {/* Faiz Oranı Display - DARK MODE UYUMLU */}
      <div style={{ 
        marginBottom: '20px', 
        padding: '20px', 
        backgroundColor: theme.colors.background, // TEMA BACKGROUND
        borderRadius: '6px',
        border: `1px solid ${theme.colors.border}` // TEMA BORDER
      }}>
        <div style={{ 
          display: 'flex', 
          flexDirection: 'column',
          alignItems: 'center', 
          justifyContent: 'center',
          gap: '15px'
        }}>
          {/* Faiz Oranı Label + Value */}
          <div style={{ textAlign: 'center' }}>
            <label style={{ 
              display: 'block', 
              marginBottom: '10px', 
              fontWeight: 'bold', 
              fontSize: '14px',
              color: theme.colors.text // TEMA TEXT
            }}>
              📊 Aktif Faiz Oranı
            </label>
            <div style={{
              padding: '15px 25px',
              backgroundColor: theme.colors.primary, // TEMA PRIMARY COLOR
              color: 'white', // HER ZAMAN BEYAZ - PRIMARY ÜSTÜNDE
              borderRadius: '8px',
              fontSize: '20px',
              fontWeight: 'bold',
              minWidth: '100px',
              display: 'inline-block'
            }}>
              %{faizOrani}
            </div>
          </div>
          
          {/* Analiz Et Butonu */}
          <button 
            onClick={handleAnalysisCalculate}
            disabled={loading}
            style={{ 
              padding: '12px 30px', 
              backgroundColor: theme.colors.success, // TEMA SUCCESS COLOR
              color: 'white', // HER ZAMAN BEYAZ - SUCCESS ÜSTÜNDE
              border: 'none', 
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '16px',
              fontWeight: 'bold',
              minWidth: '160px',
              transition: 'all 0.2s ease',
              opacity: loading ? 0.7 : 1
            }}
            onMouseOver={(e) => {
              if (!loading) {
                e.currentTarget.style.opacity = '0.9';
                e.currentTarget.style.transform = 'translateY(-1px)';
              }
            }}
            onMouseOut={(e) => {
              e.currentTarget.style.opacity = '1';
              e.currentTarget.style.transform = 'translateY(0px)';
            }}
          >
            {loading ? '⏳ Hesaplanıyor...' : '🔄 Analiz Et'}
          </button>
          
          {/* Alt Bilgi */}
          <div style={{ 
            textAlign: 'center',
            fontSize: '12px', 
            color: theme.colors.textSecondary, // TEMA SECONDARY TEXT
            fontStyle: 'italic'
          }}>
            💡 Faiz oranını değiştirmek için yukarıdaki filtreyi kullanın
          </div>
        </div>
      </div>

      {/* Aktif Filtreler */}
      <div style={{ 
        marginBottom: '20px', 
        padding: '10px', 
        backgroundColor: theme.colors.background, // TEMA BACKGROUND
        borderRadius: '6px',
        fontSize: '14px',
        border: `1px solid ${theme.colors.border}` // TEMA BORDER
      }}>
        <strong style={{ color: theme.colors.text }}>🎯 Analiz Parametreleri:</strong><br />
        <span style={{ color: theme.colors.text }}>🏢 Kuruluş: <strong>{kaynakKurulus}</strong></span><br />
        {fonNo && <><span style={{ color: theme.colors.text }}>💼 Fon: <strong>{fonNo}</strong></span><br /></>}
        {ihracNo && <><span style={{ color: theme.colors.text }}>🎯 İhraç: <strong>{ihracNo}</strong></span><br /></>}
        <span style={{ color: theme.colors.text }}>📈 Faiz Oranı: <strong>%{faizOrani}</strong></span>
      </div>

      {/* Loading State */}
      {loading && (
        <div style={{ 
          padding: '40px', 
          textAlign: 'center',
          backgroundColor: theme.colors.warning + '20', // TEMA WARNING + TRANSPARENCY
          borderRadius: '6px',
          border: `1px solid ${theme.colors.warning}`
        }}>
          <h4 style={{ color: theme.colors.text }}>⏳ Nakit Akış Analizi Hesaplanıyor...</h4>
          <p style={{ color: theme.colors.text }}>Faiz oranı: <strong>%{faizOrani}</strong></p>
        </div>
      )}

      {/* Analysis Results */}
      {!loading && analysisData && (
        <div>
          <h4 style={{ color: theme.colors.text }}>📊 Analiz Sonuçları</h4>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
            gap: '15px',
            marginTop: '15px'
          }}>
            {/* Toplam Faiz Tutarı */}
            <div style={{ 
              padding: '20px', 
              backgroundColor: theme.colors.success + '20', // TEMA SUCCESS + TRANSPARENCY
              borderRadius: '8px',
              textAlign: 'center',
              border: `1px solid ${theme.colors.success}`
            }}>
              <h4 style={{ margin: '0 0 10px 0', color: theme.colors.success }}>💚 Gerçek Faiz Tutarı</h4>
              <p style={{ 
                fontSize: '24px', 
                fontWeight: 'bold', 
                color: theme.colors.success,
                margin: '0'
              }}>
                ₺{analysisData.toplamFaizTutari.toLocaleString('tr-TR')}
              </p>
              
            </div>

            {/* Model Faiz Tutarı */}
            <div style={{ 
              padding: '20px', 
              backgroundColor: theme.colors.primary + '20', // TEMA PRIMARY + TRANSPARENCY
              borderRadius: '8px',
              textAlign: 'center',
              border: `1px solid ${theme.colors.primary}`
            }}>
              <h4 style={{ margin: '0 0 10px 0', color: theme.colors.primary }}>🎯 Model Faiz Tutarı</h4>
              <p style={{ 
                fontSize: '24px', 
                fontWeight: 'bold', 
                color: theme.colors.primary,
                margin: '0'
              }}>
                ₺{analysisData.toplamModelFaizTutari.toLocaleString('tr-TR')}
              </p>
              <small style={{ color: theme.colors.textSecondary }}>(%{faizOrani} oranında)</small>
            </div>

            {/* Fark */}
            <div style={{ 
              padding: '20px', 
              backgroundColor: analysisData.farkTutari >= 0 
                ? theme.colors.success + '20' 
                : theme.colors.error + '20',
              borderRadius: '8px',
              textAlign: 'center',
              border: `1px solid ${analysisData.farkTutari >= 0 ? theme.colors.success : theme.colors.error}`
            }}>
              <h4 style={{ 
                margin: '0 0 10px 0', 
                color: analysisData.farkTutari >= 0 ? theme.colors.success : theme.colors.error
              }}>
                {analysisData.farkTutari >= 0 ? '📈' : '📉'} Fark
              </h4>
              <p style={{ 
                fontSize: '24px', 
                fontWeight: 'bold', 
                color: analysisData.farkTutari >= 0 ? theme.colors.success : theme.colors.error,
                margin: '0'
              }}>
                ₺{analysisData.farkTutari.toLocaleString('tr-TR')}
              </p>
              <small style={{ 
                color: analysisData.farkTutari >= 0 ? theme.colors.success : theme.colors.error,
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
            backgroundColor: theme.colors.background,
            borderRadius: '6px',
            border: `1px solid ${theme.colors.border}`
          }}>
            <h4 style={{ color: theme.colors.text }}>📋 Analiz Özeti</h4>
            <ul style={{ margin: '0', paddingLeft: '20px', color: theme.colors.text }}>
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
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          border: `1px solid ${theme.colors.border}`
        }}>
          <h4 style={{ color: theme.colors.text }}>💡 Nakit Akış Analizi</h4>
          <p style={{ color: theme.colors.text }}><strong>"Analiz Et"</strong> butonuna tıklayarak başlayın.</p>
          <p style={{ fontSize: '12px', color: theme.colors.textSecondary }}>
            Faiz oranı: <strong>%{faizOrani}</strong> (Yukarıdaki filtreden değiştirilebilir)
          </p>
        </div>
      )}
    </div>
  );
};

export default AnalysisChart;