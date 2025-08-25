import React, { useState, useEffect } from 'react';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  ChartOptions,
} from 'chart.js';
import { CashFlowAnalysisData } from '../../types/nakitAkis';
import { useTheme } from '../../contexts/ThemeContext';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface CashFlowAnalysisChartProps {
  data: CashFlowAnalysisData[];
  currentPeriod?: string;
  onPeriodChange?: (period: string) => void;
}

const CashFlowAnalysisChart: React.FC<CashFlowAnalysisChartProps> = ({ 
  data, 
  currentPeriod = 'month',
  onPeriodChange 
}) => {
  const { theme } = useTheme();
  const [selectedMetric, setSelectedMetric] = useState<'anapara' | 'kazanc' | 'verimlilik' | 'performance'>('kazanc');
  
  // PARENT'TAN GELEN PERIOD'U KULLAN
  const [selectedPeriod, setSelectedPeriod] = useState<string>(currentPeriod);
  
  // PARENT'TAN PERIOD DEĞİŞTİĞİNDE SYNC ET
  useEffect(() => {
    setSelectedPeriod(currentPeriod);
  }, [currentPeriod]);
  
  // DEBUG: Gelen veriyi kontrol et
  useEffect(() => {
    if (data && data.length > 0) {
      console.log('=== CASH FLOW DATA DEBUG ===');
      console.log('Current period:', currentPeriod);
      console.log('Data length:', data.length);
      console.log('First item:', data[0]);
      console.log('========================');
    }
  }, [data, currentPeriod]);
  
  if (!data || data.length === 0) {
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center', 
        border: `2px dashed ${theme.colors.border}`,
        borderRadius: '8px',
        backgroundColor: theme.colors.background
      }}>
        <h3 style={{ color: theme.colors.text }}>💹 Cash Flow Analiz Verisi Yok</h3>
        <p style={{ color: theme.colors.textSecondary }}>
          cash_flow_analysis tablosunda gösterilecek veri bulunamadı.
        </p>
      </div>
    );
  }

  // Period değişikliği
  const handlePeriodChange = (period: string) => {
    setSelectedPeriod(period);
    if (onPeriodChange) {
      onPeriodChange(period);
    }
  };

  // TARİH FORMATLAMA - currentPeriod KULLAN
  const labels = data.map((item: CashFlowAnalysisData) => {
    const date = new Date(item.timestamp);
    
    switch (currentPeriod) {
      case 'day':
        return date.toLocaleDateString('tr-TR', { 
          day: '2-digit', 
          month: '2-digit'
        });
      case 'week':
        return date.toLocaleDateString('tr-TR', { 
          day: '2-digit', 
          month: '2-digit'
        }) + ' Hf';
      case 'month':
        return date.toLocaleDateString('tr-TR', { 
          month: '2-digit',
          year: '2-digit'
        });
      default:
        return date.toLocaleDateString('tr-TR');
    }
  });

  // ANAPARA grafiği verisi
  const anaparaChartData = {
    labels,
    datasets: [
      {
        label: '💰 Toplam Anapara (₺)',
        data: data.map((item: CashFlowAnalysisData) => item.total_anapara),
        borderColor: '#6c757d',
        backgroundColor: 'rgba(108, 117, 125, 0.1)',
        borderWidth: 4,
        pointBackgroundColor: '#6c757d',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: true,
        tension: 0.4
      }
    ]
  };

  // Kazanç grafiği verisi
  const kazancChartData = {
    labels,
    datasets: [
      {
        label: '💰 Basit Faiz Kazancı (₺)',
        data: data.map((item: CashFlowAnalysisData) => item.total_faiz_kazanci),
        borderColor: '#28a745',
        backgroundColor: 'rgba(40, 167, 69, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#28a745',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      },
      {
        label: '🎯 Model Faiz Kazancı (₺)',
        data: data.map((item: CashFlowAnalysisData) => item.total_model_faiz_kazanci),
        borderColor: '#007bff',
        backgroundColor: 'rgba(0, 123, 255, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#007bff',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      },
      {
        label: '📊 TLREF Faiz Kazancı (₺)',
        data: data.map((item: CashFlowAnalysisData) => item.total_tlref_kazanci),
        borderColor: '#fa0505ff',
        backgroundColor: 'rgba(111, 66, 193, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#fa0505ff',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      }
    ]
  };

  // VERİMLİLİK grafiği verisi
  const verimlilikChartData = {
    labels,
    datasets: [
      {
        label: '💰 Basit Faiz Verimlilik (%)',
        data: data.map((item: CashFlowAnalysisData) => item.basit_faiz_yield_percentage || 0),
        borderColor: '#28a745',
        backgroundColor: 'rgba(40, 167, 69, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#28a745',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      },
      {
        label: '🎯 Model Faiz Verimlilik (%)',
        data: data.map((item: CashFlowAnalysisData) => item.model_faiz_yield_percentage || 0),
        borderColor: '#007bff',
        backgroundColor: 'rgba(0, 123, 255, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#007bff',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      },
      {
        label: '📊 TLREF Faiz Verimlilik (%)',
        data: data.map((item: CashFlowAnalysisData) => item.tlref_faiz_yield_percentage || 0),
        borderColor: '#6f42c1',
        backgroundColor: 'rgba(111, 66, 193, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#6f42c1',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      }
    ]
  };

  // Performans grafiği verisi
  const performanceChartData = {
    labels,
    datasets: [
      {
        label: '📈 Basit vs Model Performans (%)',
        data: data.map((item: CashFlowAnalysisData) => item.basit_vs_model_performance),
        borderColor: '#fd7e14',
        backgroundColor: 'rgba(253, 126, 20, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#fd7e14',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      },
      {
        label: '📊 Basit vs TLREF Performans (%)',
        data: data.map((item: CashFlowAnalysisData) => item.basit_vs_tlref_performance),
        borderColor: '#dc3545',
        backgroundColor: 'rgba(220, 53, 69, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#dc3545',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: false,
        tension: 0.4
      }
    ]
  };

  const chartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      intersect: false,
      mode: 'index'
    },
    plugins: {
      legend: {
        position: 'top',
        labels: {
          usePointStyle: true,
          padding: 20,
          color: theme.colors.text,
          font: {
            size: 15,
            weight: 'bold' as const
          }
        }
      },
      title: {
        display: true,
        text: selectedMetric === 'anapara' 
          ? '💰 Cash Flow Analizi - Koçfinans - Anapara Dağılımı'
          : selectedMetric === 'kazanc'
          ? '💹 Cash Flow Analizi - Koçfinans - Faiz Kazançları'
          : selectedMetric === 'verimlilik'
          ? '📊 Cash Flow Analizi - Anapara Verimliliği (%)'
          : '📊 Cash Flow Analizi - Performans Karşılaştırma',
        color: theme.colors.text,
        font: {
          size: 16,
          weight: 'bold' as const
        },
        padding: {
          top: 10,
          bottom: 30
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.9)',
        titleColor: '#ffffff',
        bodyColor: '#ffffff',
        borderColor: theme.colors.primary,
        borderWidth: 2,
        titleFont: {
          size: 14,
          weight: 'bold' as const
        },
        bodyFont: {
          size: 13
        },
        padding: 15,
        cornerRadius: 8,
        displayColors: true,
        callbacks: {
          title: function(context: any) {
            try {
              const dataIndex = context[0].dataIndex;
              const item = data[dataIndex];
              const recordCount = item?.record_count || 0;
              
              // TARİH FORMATINI PERIOD'A GÖRE AYARLA
              const date = new Date(item.timestamp);
              let dateStr = '';
              
              switch (currentPeriod) {
                case 'day':
                  dateStr = date.toLocaleDateString('tr-TR', { 
                    day: '2-digit', 
                    month: '2-digit', 
                    year: 'numeric' 
                  });
                  break;
                case 'week':
                  dateStr = date.toLocaleDateString('tr-TR', { 
                    day: '2-digit', 
                    month: '2-digit', 
                    year: 'numeric' 
                  }) + ' Haftası';
                  break;
                case 'month':
                  dateStr = date.toLocaleDateString('tr-TR', { 
                    month: 'long', 
                    year: 'numeric' 
                  });
                  break;
                default:
                  dateStr = date.toLocaleDateString('tr-TR');
              }
              
              return `📅 ${dateStr} (${recordCount} kayıt)`;
            } catch (error) {
              console.error('Tooltip title error:', error);
              return '📅 Tarih bilgisi yüklenemiyor';
            }
          },
          beforeBody: function(context: any) {
            try {
              const dataIndex = context[0].dataIndex;
              const item = data[dataIndex];
              
              if (!item) return [];
              
              const tooltipLines = [
                `💰 Anapara: ₺${(item.total_anapara || 0).toLocaleString('tr-TR')}`
              ];
              
              // Basit faiz oranı - güvenli kontrol
              if (item.avg_basit_faiz !== undefined && item.avg_basit_faiz !== null && !isNaN(item.avg_basit_faiz)) {
                tooltipLines.push(`🟢 Basit Faiz Oranı: %${item.avg_basit_faiz.toFixed(4)}`);
              }
              
              // Model nema oranı - güvenli kontrol
              if (item.avg_model_nema_orani !== undefined && item.avg_model_nema_orani !== null && !isNaN(item.avg_model_nema_orani)) {
                tooltipLines.push(`📊 Model Nema: %${item.avg_model_nema_orani.toFixed(4)}`);
              }
              
              // TLREF oranı - güvenli kontrol
              if (item.avg_tlref_faiz !== undefined && item.avg_tlref_faiz !== null && !isNaN(item.avg_tlref_faiz)) {
                tooltipLines.push(`📈 TLREF Oranı: %${(item.avg_tlref_faiz * 100).toFixed(4)}`);
              }
              
              return tooltipLines;
            } catch (error) {
              console.error('Tooltip beforeBody error:', error);
              return ['💰 Veri bilgisi yüklenemiyor'];
            }
          },
          label: function(context: any) {
            try {
              const value = context.parsed.y || 0;
              
              if (selectedMetric === 'anapara') {
                return `${context.dataset.label}: ₺${value.toLocaleString('tr-TR')}`;
              } else if (selectedMetric === 'kazanc') {
                return `${context.dataset.label}: ₺${value.toLocaleString('tr-TR')}`;
              } else if (selectedMetric === 'verimlilik') {
                return `${context.dataset.label}: %${value.toFixed(2)}`;
              } else {
                return `${context.dataset.label}: %${value.toFixed(2)}`;
              }
            } catch (error) {
              console.error('Tooltip label error:', error);
              return `${context.dataset.label}: Veri hatası`;
            }
          },
          afterBody: function(context: any) {
            try {
              const dataIndex = context[0].dataIndex;
              const item = data[dataIndex];
              
              if (!item) return [];
              
              if (selectedMetric === 'verimlilik') {
                return [
                  '',
                  `💡 Basit Faiz Kazancı: ₺${(item.total_faiz_kazanci || 0).toLocaleString('tr-TR')}`,
                  `🎯 Model Faiz Kazancı: ₺${(item.total_model_faiz_kazanci || 0).toLocaleString('tr-TR')}`,
                  `📊 TLREF Faiz Kazancı: ₺${(item.total_tlref_kazanci || 0).toLocaleString('tr-TR')}`
                ];
              } else if (selectedMetric === 'performance') {
                return [
                  '',
                  `💡 Basit Faiz Toplamı: ₺${(item.total_faiz_kazanci || 0).toLocaleString('tr-TR')}`,
                  `🎯 Model Faiz Toplamı: ₺${(item.total_model_faiz_kazanci || 0).toLocaleString('tr-TR')}`,
                  `📊 TLREF Faiz Toplamı: ₺${(item.total_tlref_kazanci || 0).toLocaleString('tr-TR')}`
                ];
              }
              return [];
            } catch (error) {
              console.error('Tooltip afterBody error:', error);
              return [];
            }
          }
        }
      }
    },
    scales: {
      x: {
        title: {
          display: true,
          text: `📅 ${currentPeriod === 'day' ? 'Günlük' : currentPeriod === 'week' ? 'Haftalık' : 'Aylık'} Periyotlar (Eskiden Yeniye)`,
          color: theme.colors.text,
          font: {
            size: 12,
            weight: 'bold' as const
          }
        },
        grid: {
          color: theme.colors.border
        },
        ticks: {
          color: theme.colors.text
        }
      },
      y: {
        title: {
          display: true,
          text: selectedMetric === 'anapara' 
            ? '💰 Anapara (₺)'
            : selectedMetric === 'kazanc' 
            ? '💰 Faiz Kazancı (₺)'
            : selectedMetric === 'verimlilik'
            ? '📊 Verimlilik (%)'
            : '📊 Performans (%)',
          color: theme.colors.text,
          font: {
            size: 12,
            weight: 'bold' as const
          }
        },
        grid: {
          color: theme.colors.border
        },
        ticks: {
          color: theme.colors.text,
          callback: function(value: any) {
            if (selectedMetric === 'performance' || selectedMetric === 'verimlilik') {
              return '%' + value.toFixed(1);
            } else {
              return '₺' + value.toLocaleString('tr-TR');
            }
          }
        }
      }
    }
  };

  // Özet istatistikler
  const totalBasitKazanc = data.reduce((sum: number, item: CashFlowAnalysisData) => sum + (item.total_faiz_kazanci || 0), 0);
  const totalModelKazanc = data.reduce((sum: number, item: CashFlowAnalysisData) => sum + (item.total_model_faiz_kazanci || 0), 0);
  const totalTlrefKazanc = data.reduce((sum: number, item: CashFlowAnalysisData) => sum + (item.total_tlref_kazanci || 0), 0);
  const avgModelPerformance = data.reduce((sum: number, item: CashFlowAnalysisData) => sum + (item.basit_vs_model_performance || 0), 0) / data.length;

  return (
    <div style={{ 
      padding: '20px',
      backgroundColor: theme.colors.surface,
      borderRadius: '8px',
      border: `1px solid ${theme.colors.border}`,
      boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
    }}>
      {/* Period Seçici */}
      <div style={{ 
        marginBottom: '20px',
        display: 'flex',
        gap: '10px',
        justifyContent: 'center',
        flexWrap: 'wrap'
      }}>
        <div style={{ 
          padding: '10px', 
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          border: `1px solid ${theme.colors.border}`
        }}>
          <label style={{ marginRight: '10px', color: theme.colors.text, fontWeight: 'bold' }}>
            📅 Periyot:
          </label>
          <select 
            value={selectedPeriod}
            onChange={(e) => handlePeriodChange(e.target.value)}
            style={{
              padding: '5px 10px',
              borderRadius: '4px',
              border: `1px solid ${theme.colors.border}`,
              backgroundColor: theme.colors.background,
              color: theme.colors.text
            }}
          >
            <option value="day">📆 Günlük</option>
            <option value="week">📅 Haftalık</option>
            <option value="month">📊 Aylık</option>
          </select>
        </div>
      </div>

      {/* Metrik seçici */}
      <div style={{ 
        marginBottom: '20px',
        display: 'flex',
        gap: '10px',
        justifyContent: 'center',
        flexWrap: 'wrap'
      }}>
         <button
          onClick={() => setSelectedMetric('anapara')}
          style={{
            padding: '10px 20px',
            backgroundColor: selectedMetric === 'anapara' ? theme.colors.warning : theme.colors.background,
            color: selectedMetric === 'anapara' ? 'white' : theme.colors.text,
            border: `1px solid ${theme.colors.border}`,
            borderRadius: '6px',
            cursor: 'pointer',
            fontWeight: 'bold'
          }}
        >
          💰 Anapara
        </button>
        <button
          onClick={() => setSelectedMetric('kazanc')}
          style={{
            padding: '10px 20px',
            backgroundColor: selectedMetric === 'kazanc' ? theme.colors.primary : theme.colors.background,
            color: selectedMetric === 'kazanc' ? 'white' : theme.colors.text,
            border: `1px solid ${theme.colors.border}`,
            borderRadius: '6px',
            cursor: 'pointer',
            fontWeight: 'bold'
          }}
        >
          💹 Faiz Kazançları
        </button>
       {/*} <button
          onClick={() => setSelectedMetric('verimlilik')}
          style={{
            padding: '10px 20px',
            backgroundColor: selectedMetric === 'verimlilik' ? theme.colors.warning : theme.colors.background,
            color: selectedMetric === 'verimlilik' ? 'white' : theme.colors.text,
            border: `1px solid ${theme.colors.border}`,
            borderRadius: '6px',
            cursor: 'pointer',
            fontWeight: 'bold'
          }}
        >
          📊 Verimlilik (%)
        </button>
        <button
          onClick={() => setSelectedMetric('performance')}
          style={{
            padding: '10px 20px',
            backgroundColor: selectedMetric === 'performance' ? theme.colors.success : theme.colors.background,
            color: selectedMetric === 'performance' ? 'white' : theme.colors.text,
            border: `1px solid ${theme.colors.border}`,
            borderRadius: '6px',
            cursor: 'pointer',
            fontWeight: 'bold'
          }}
        >
          📈 Performans (%)
        </button>*/}
      </div> 
    
      {/* Özet kartlar - 4 KART (ANAPARA KALDIRILDI) */}
      <div style={{ 
        marginBottom: '20px',
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
        gap: '15px'
      }}>
        <div style={{ 
          textAlign: 'center',
          padding: '15px',
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          border: `1px solid ${theme.colors.success}`
        }}>
          <div style={{ fontSize: '18px', fontWeight: 'bold', color: theme.colors.success }}>
            ₺{totalBasitKazanc.toLocaleString('tr-TR')}
          </div>
          <div style={{ fontSize: '14px', color: theme.colors.text }}>Basit Faiz Kazancı</div>
        </div>
        
        <div style={{ 
          textAlign: 'center',
          padding: '15px',
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          border: `1px solid ${theme.colors.primary}`
        }}>
          <div style={{ fontSize: '18px', fontWeight: 'bold', color: theme.colors.primary }}>
            ₺{totalModelKazanc.toLocaleString('tr-TR')}
          </div>
          <div style={{ fontSize: '14px', color: theme.colors.text }}>Model Faiz Kazancı</div>
        </div>
        
        <div style={{ 
          textAlign: 'center',
          padding: '15px',
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          border: `1px solid ${theme.colors.error}`
        }}>
          <div style={{ fontSize: '18px', fontWeight: 'bold', color: theme.colors.error }}>
            ₺{totalTlrefKazanc.toLocaleString('tr-TR')}
          </div>
          <div style={{ fontSize: '14px', color: theme.colors.text }}>TLREF Faiz Kazancı</div>
        </div>
        
        <div style={{ 
          textAlign: 'center',
          padding: '15px',
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          border: `1px solid ${avgModelPerformance >= 0 ? theme.colors.success : theme.colors.error}`
        }}>
          <div style={{ 
            fontSize: '18px', 
            fontWeight: 'bold', 
            color: avgModelPerformance >= 0 ? theme.colors.success : theme.colors.error 
          }}>
            {avgModelPerformance >= 0 ? '+' : ''}{avgModelPerformance.toFixed(1)}%
          </div>
          <div style={{ fontSize: '14px', color: theme.colors.text }}>Ort. Model Performans</div>
        </div>
      </div>

      {/* Grafik */}
      <div style={{ height: '400px' }}>
        <Line 
          data={selectedMetric === 'anapara' ? anaparaChartData 
                : selectedMetric === 'kazanc' ? kazancChartData 
                : selectedMetric === 'verimlilik' ? verimlilikChartData 
                : performanceChartData} 
          options={chartOptions} 
        />
      </div>
      
      {/* Veri bilgisi */}
      <div style={{ 
        marginTop: '20px',
        fontSize: '16px',
        color: theme.colors.textSecondary,
        textAlign: 'center'
      }}>
        💹 <strong>{data.length}</strong> {currentPeriod === 'day' ? 'günlük' : currentPeriod === 'week' ? 'haftalık' : 'aylık'} veri gösteriliyor | 
        📅 <strong>{labels[0]}</strong> - <strong>{labels[labels.length - 1]}</strong> dönemi |
        📊 Toplam kayıt: <strong>{data.reduce((sum: number, item: CashFlowAnalysisData) => sum + (item.record_count || 0), 0).toLocaleString('tr-TR')}</strong>
      </div>
    </div>
  );
};

export default CashFlowAnalysisChart;