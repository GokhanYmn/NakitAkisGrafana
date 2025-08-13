import React, { useState } from 'react';
import { useTheme } from '../../contexts/ThemeContext';

interface ExportMenuProps {
  analysisData: any;
  filters: {
    kaynakKurulus: string;
    fonNo: string;
    ihracNo: string;
    faizOrani: number;
  };
}

const ExportMenu: React.FC<ExportMenuProps> = ({ analysisData, filters }) => {
  const { theme } = useTheme();
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState<string | null>(null);

  const exportLevels = [
    {
      id: 'basic',
      name: '📋 Basit Özet',
      description: 'Temel analiz sonuçları',
      icon: '📋'
    },
    {
      id: 'detailed',
      name: '📊 Detaylı Analiz', 
      description: 'İstatistikler + breakdown',
      icon: '📊'
    },
    {
      id: 'full',
      name: '📈 Tam Rapor',
      description: 'Görseller + öneriler',
      icon: '📈'
    }
  ];

  const handleExport = async (level: string, format: 'excel' | 'pdf') => {
    setLoading(`${level}-${format}`);
    try {
      console.log(`Exporting ${level} as ${format}...`);
      
      const params = new URLSearchParams({
        level: level,
        format: format,
        faizOrani: filters.faizOrani.toString(),
        kaynak_kurulus: filters.kaynakKurulus,
        ...(filters.fonNo && { fm_fonlar: filters.fonNo }),
        ...(filters.ihracNo && { ihrac_no: filters.ihracNo })
      });

      const url = `http://localhost:7289/api/grafana/export?${params}`;
      console.log('Export URL:', url);

      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Accept': format === 'excel' ? 'text/csv' : 'text/html',
        }
      });

      console.log('Response status:', response.status);
      console.log('Response headers:', response.headers);

      if (response.ok) {
        const blob = await response.blob();
        console.log('Blob size:', blob.size);
        
        const downloadUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = downloadUrl;
        a.download = `nakit_akis_${level}_${filters.kaynakKurulus}_${new Date().toISOString().slice(0,10)}.${format === 'excel' ? 'csv' : 'html'}`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(downloadUrl);
        document.body.removeChild(a);
        
        console.log('Export başarılı!');
      } else {
        const errorText = await response.text();
        console.error('Export failed - Status:', response.status);
        console.error('Export failed - Response:', errorText);
        alert(`Export hatası: ${response.status} - ${errorText}`);
      }
    } catch (error: any) {
      console.error('Export error:', error);
      console.error('Error details:', {
        message: error.message,
        stack: error.stack,
        url: `http://localhost:7289/api/grafana/export`
      });
      alert(`Export hatası: ${error.message}`);
    } finally {
      setLoading(null);
    }
  };

  return (
    <div style={{ position: 'relative', display: 'inline-block' }}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        style={{
          padding: '10px 16px',
          backgroundColor: theme.colors.primary,
          color: 'white',
          border: 'none',
          borderRadius: '6px',
          cursor: 'pointer',
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          fontSize: '14px',
          fontWeight: '500'
        }}
      >
        📥 Export Raporu
        <span style={{ transform: isOpen ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform 0.2s' }}>
          ▼
        </span>
      </button>

      {isOpen && (
        <div style={{
          position: 'absolute',
          top: '100%',
          left: 0,
          backgroundColor: theme.colors.surface,
          border: `1px solid ${theme.colors.border}`,
          borderRadius: '8px',
          boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
          zIndex: 1000,
          minWidth: '280px',
          padding: '8px'
        }}>
          {exportLevels.map((level) => (
            <div
              key={level.id}
              style={{
                padding: '12px',
                borderBottom: level.id !== 'full' ? `1px solid ${theme.colors.border}` : 'none'
              }}
            >
              <div style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                marginBottom: '8px'
              }}>
                <div>
                  <div style={{ 
                    fontSize: '14px', 
                    fontWeight: 'bold',
                    color: theme.colors.text 
                  }}>
                    {level.name}
                  </div>
                  <div style={{ 
                    fontSize: '12px', 
                    color: theme.colors.textSecondary 
                  }}>
                    {level.description}
                  </div>
                </div>
              </div>
              
              {/* SADECE PDF EXPORT BUTONU */}
              <div style={{ display: 'flex', gap: '8px', justifyContent: 'center' }}>
                <button
                  onClick={() => handleExport(level.id, 'pdf')}
                  disabled={loading === `${level.id}-pdf`}
                  style={{
                    flex: 1,
                    padding: '8px 16px',
                    backgroundColor: '#dc3545',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '14px',
                    fontWeight: '500',
                    minWidth: '120px'
                  }}
                >
                  {loading === `${level.id}-pdf` ? '⏳ İndiriliyor...' : '📄 PDF İndir'}
                </button>
              </div>
              
              {/* EXCEL BUTONU KALDIRILDI */}
              {/* Excel backend kodları duruyor, sadece UI'den kaldırıldı */}
            </div>
          ))}
          
          {/* Bilgi notu */}
          <div style={{
            padding: '8px 12px',
            fontSize: '11px',
            color: theme.colors.textSecondary,
            textAlign: 'center',
            borderTop: `1px solid ${theme.colors.border}`,
            fontStyle: 'italic'
          }}>
            💡 Excel export geçici olarak devre dışı
          </div>
        </div>
      )}
    </div>
  );
};

export default ExportMenu;