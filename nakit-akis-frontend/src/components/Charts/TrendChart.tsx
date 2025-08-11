import React from "react";
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    PointElement,
    Title,
    Tooltip,
    Legend,
    LineElement,
} from 'chart.js';
import { Line } from 'react-chartjs-2';
import { TrendData } from "../../types/nakitAkis";

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
    data: TrendData[];
    kurulus: string;
}

const TrendChart: React.FC<TrendChartProps> = ({ data, kurulus }) => {
  if (!data) {
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center', 
        border: '2px dashed #ddd', 
        borderRadius: '8px',
        backgroundColor: '#f9f9f9'
      }}>
        <h3>📊 Veri Yükleniyor...</h3>
        <p>Trend verileri hazırlanıyor...</p>
      </div>
    );
  }

  if (!Array.isArray(data)) {
    console.error('TrendChart: data is not an array:', data);
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center', 
        border: '2px dashed #orange', 
        borderRadius: '8px',
        backgroundColor: '#fff3cd'
      }}>
        <h3>⚠️ Veri Format Hatası</h3>
        <p>Trend verileri beklenmeyen formatta.</p>
        <button 
          onClick={() => window.location.reload()}
          style={{ 
            padding: '8px 16px', 
            backgroundColor: '#ffc107', 
            color: 'black', 
            border: 'none', 
            borderRadius: '4px' 
          }}
        >
          🔄 Sayfayı Yenile
        </button>
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div style={{ 
        padding: '40px', 
        textAlign: 'center', 
        border: '2px dashed #ddd', 
        borderRadius: '8px',
        backgroundColor: '#f9f9f9'
      }}>
        <h3>📊 Trend Verisi Bekleniyor</h3>
        <p>Kuruluş seçildikten sonra trend grafiği burada görünecek...</p>
      </div>
    );
  }

  const labels = data.map(d => new Date(d.timestamp).toLocaleDateString('tr-TR'));
  
  const chartData = {
    labels,
    datasets: [
      {
        label: 'Kümülatif Mevduat (₺)',
        data: data.map(d => d.kumulatif_mevduat),
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
        borderWidth: 3,
        tension: 0.1,
        yAxisID: 'y',
      },
      {
        label: 'Kümülatif Faiz Kazancı (₺)',
        data: data.map(d => d.kumulatif_faiz_kazanci),
        borderColor: 'rgb(255, 99, 132)',
        backgroundColor: 'rgba(255, 99, 132, 0.2)',
        borderWidth: 3,
        tension: 0.1,
        yAxisID: 'y1',
      }
    ],
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index' as const,
      intersect: false,
    },
    plugins: {
      legend: {
        position: 'top' as const,
      },
      title: {
        display: true,
        text: `📈 ${kurulus} - Haftalık Trend Analizi`,
        font: {
          size: 16
        }
      },
      tooltip: {
        callbacks: {
          label: function(context: any) {
            let label = context.dataset.label || '';
            if (label) {
              label += ': ';
            }
            label += '₺' + Number(context.parsed.y).toLocaleString('tr-TR');
            return label;
          }
        }
      }
    },
    scales: {
      x: {
        display: true,
        title: {
          display: true,
          text: 'Tarih'
        }
      },
      y: {
        type: 'linear' as const,
        display: true,
        position: 'left' as const,
        title: {
          display: true,
          text: 'Mevduat (₺)',
          color: 'rgb(75, 192, 192)'
        },
        ticks: {
          callback: function(value: any) {
            return '₺' + Number(value).toLocaleString('tr-TR');
          },
          color: 'rgb(75, 192, 192)'
        }
      },
      y1: {
        type: 'linear' as const,
        display: true,
        position: 'right' as const,
        title: {
          display: true,
          text: 'Faiz Kazancı (₺)',
          color: 'rgb(255, 99, 132)'
        },
        grid: {
          drawOnChartArea: false,
        },
        ticks: {
          callback: function(value: any) {
            return '₺' + Number(value).toLocaleString('tr-TR');
          },
          color: 'rgb(255, 99, 132)'
        }
      }
    },
  };

  return (
    <div style={{ 
      padding: '20px', 
      border: '2px solid #ddd', 
      borderRadius: '8px', 
      backgroundColor: 'white',
      marginTop: '20px',
      height: '510px'
    }}>
      <Line data={chartData} options={options} />
      <div style={{ marginTop: '1px', fontSize: '16px', color: '#666' }}>
        📊 Toplam {data.length} haftalık veri • Son güncelleme: {new Date().toLocaleString('tr-TR')}
      </div>
    </div>
  );
};

export default TrendChart;