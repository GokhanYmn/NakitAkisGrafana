import React from 'react';
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

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface TrendChartProps {
  data: any[];
  kurulus: string;
}

const TrendChart: React.FC<TrendChartProps> = ({ data, kurulus }) => {
  if (!data || data.length === 0) {
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center', 
        border: '2px dashed #ddd',
        borderRadius: '8px',
        backgroundColor: '#f9f9f9'
      }}>
        <h3>📊 Veri Bulunamadı</h3>
        <p>Seçili kuruluş için haftalık trend verisi bulunmuyor.</p>
      </div>
    );
  }

  // Tarihleri formatla
  const labels = data.map(item => {
    const date = new Date(item.timestamp);
    return date.toLocaleDateString('tr-TR', { 
      day: '2-digit', 
      month: '2-digit' 
    });
  });

  // SADECE FAİZ KAZANCI VERİSİ
  const chartData = {
    labels,
    datasets: [
      {
        label: '💰 Kümülatif Faiz Kazancı (₺)',
        data: data.map(item => parseFloat(item.kumulatif_faiz_kazanci) || 0),
        borderColor: '#28a745',
        backgroundColor: 'rgba(40, 167, 69, 0.1)',
        borderWidth: 3,
        pointBackgroundColor: '#28a745',
        pointBorderColor: '#ffffff',
        pointBorderWidth: 2,
        pointRadius: 6,
        pointHoverRadius: 8,
        fill: true,
        tension: 0.4
      }
    ]
  };

  // TYPE SAFE OPTIONS
  const options: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top',
        labels: {
          usePointStyle: true,
          padding: 20,
          font: {
            size: 12,
            weight: 'bold' as const // TYPE SAFE
          }
        }
      },
      title: {
        display: true,
        text: `📈 ${kurulus} - Haftalık Faiz Kazancı Trendi`,
        font: {
          size: 16,
          weight: 'bold' as const // TYPE SAFE
        },
        padding: {
          top: 10,
          bottom: 30
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleColor: '#ffffff',
        bodyColor: '#ffffff',
        borderColor: '#28a745',
        borderWidth: 1,
        callbacks: {
          label: function(context: any) {
            const value = context.parsed.y;
            return `💰 Faiz Kazancı: ₺${value.toLocaleString('tr-TR')}`;
          }
        }
      }
    },
    scales: {
      x: {
        title: {
          display: true,
          text: '📅 Haftalık Periyotlar',
          font: {
            size: 12,
            weight: 'bold' as const // TYPE SAFE
          }
        },
        grid: {
          color: 'rgba(0, 0, 0, 0.1)'
        }
      },
      y: {
        title: {
          display: true,
          text: '💰 Kümülatif Faiz Kazancı (₺)',
          font: {
            size: 12,
            weight: 'bold' as const // TYPE SAFE
          }
        },
        grid: {
          color: 'rgba(0, 0, 0, 0.1)'
        },
        ticks: {
          callback: function(value: any) {
            return '₺' + value.toLocaleString('tr-TR');
          }
        }
      }
    },
    interaction: {
      intersect: false,
      mode: 'index'
    }
  };

  return (
    <div style={{ 
      padding: '20px',
      backgroundColor: 'white',
      borderRadius: '8px',
      border: '1px solid #ddd',
      boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
    }}>
      {/* Summary Info */}
      <div style={{ 
        marginBottom: '20px',
        padding: '15px',
        backgroundColor: '#f8f9fa',
        borderRadius: '6px',
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
        gap: '15px'
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#28a745' }}>
            ₺{data[data.length - 1]?.kumulatif_faiz_kazanci?.toLocaleString('tr-TR') || '0'}
          </div>
          <div style={{ fontSize: '12px', color: '#666' }}>Son Hafta Toplam Faiz</div>
        </div>
        
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#007bff' }}>
            {data.length}
          </div>
          <div style={{ fontSize: '12px', color: '#666' }}>Haftalık Veri Noktası</div>
        </div>
        
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#6f42c1' }}>
            %{data.length > 1 ? 
              (((parseFloat(data[data.length - 1]?.kumulatif_faiz_kazanci || '0') - 
                 parseFloat(data[0]?.kumulatif_faiz_kazanci || '0')) / 
                 parseFloat(data[0]?.kumulatif_faiz_kazanci || '1')) * 100).toFixed(1) 
              : '0'}
          </div>
          <div style={{ fontSize: '12px', color: '#666' }}>Dönem Büyüme</div>
        </div>
      </div>

      {/* Chart */}
      <div style={{ height: '400px' }}>
        <Line data={chartData} options={options} />
      </div>
      
      {/* Data Points Info */}
      <div style={{ 
        marginTop: '20px',
        fontSize: '12px',
        color: '#666',
        textAlign: 'center'
      }}>
        📊 <strong>{data.length}</strong> haftalık veri gösteriliyor | 
        📅 <strong>{labels[0]}</strong> - <strong>{labels[labels.length - 1]}</strong> dönemi
      </div>
    </div>
  );
};

export default TrendChart;