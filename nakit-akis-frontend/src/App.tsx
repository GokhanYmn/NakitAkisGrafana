// src/App.tsx
import React, { useState } from 'react';
import './App.css';
import Navbar from './components/Layout/Navbar';
import LayoutContainer from './components/Layout/LayoutContainer';
import HealthCheck from './components/Dashboard/HealthCheck';
import AdvancedVariableSelector from './components/Variables/AdvancedVariableSelector';
import DashboardContainer from './components/Dashboard/DashboardContainer';
import { useTheme } from './contexts/ThemeContext';

interface SelectionState {
  dashboardType: string;
  kaynakKurulus: string;
  fonNo: string;
  ihracNo: string;
  faizOrani: number | null;
}

function App() {
  const { theme } = useTheme();
  const [selection, setSelection] = useState<SelectionState>({
    dashboardType: 'trends',
    kaynakKurulus: '',
    fonNo: '',
    ihracNo: '',
    faizOrani: null
  });

  const handleSelectionChange = React.useCallback((newSelection: SelectionState) => {
    console.log('Dashboard selection changed:', newSelection);
    setSelection(newSelection);
  }, []);

  const handleDashboardChange = (dashboardType: string) => {
    setSelection(prev => ({ ...prev, dashboardType }));
  };

  // FAİZ ORANI KONTROLÜ
  const isFaizOraniValid = selection.faizOrani !== null && selection.faizOrani > 0;

  // Sidebar Content
  const renderSidebar = () => (
    <div>
      <AdvancedVariableSelector onSelectionChange={handleSelectionChange} />
      <HealthCheck />
    </div>
  );

  // Right Panel Content (Optional)
  const renderRightPanel = () => (
    <div style={{ color: theme.colors.text }}>
      <h4 style={{ marginTop: 0 }}>📊 Dashboard Özeti</h4>
      
      <div style={{ 
        padding: '15px', 
        backgroundColor: theme.colors.background,
        borderRadius: '8px',
        border: `1px solid ${theme.colors.border}`,
        marginBottom: '15px'
      }}>
        <div style={{ fontSize: '14px', color: theme.colors.textSecondary }}>
          Aktif Dashboard
        </div>
        <div style={{ fontWeight: 'bold', fontSize: '16px' }}>
          {selection.dashboardType === 'trends' && '📈 Trend Analizi'}
          {selection.dashboardType === 'analysis' && '💰 Nakit Akış Analizi'}
          {selection.dashboardType === 'cash-flow-analysis' && '💹 Cash Flow Analizi'}
          {selection.dashboardType === 'historical' && '📊 Geçmiş Veriler'}
          {selection.dashboardType === 'comparison' && '⚖️ Kuruluş Karşılaştırma'}
        </div>
      </div>

      <div style={{ 
        padding: '15px', 
        backgroundColor: theme.colors.background,
        borderRadius: '8px',
        border: `1px solid ${theme.colors.border}`,
        marginBottom: '15px'
      }}>
        <div style={{ fontSize: '14px', color: theme.colors.textSecondary }}>
          Seçili Kuruluş
        </div>
        <div style={{ fontWeight: 'bold' }}>
          {selection.kaynakKurulus || 'Seçilmemiş'}
        </div>
        
        {selection.fonNo && (
          <>
            <div style={{ fontSize: '14px', color: theme.colors.textSecondary, marginTop: '8px' }}>
              Fon Numarası
            </div>
            <div style={{ fontWeight: 'bold' }}>
              {selection.fonNo}
            </div>
          </>
        )}
        
        {selection.ihracNo && (
          <>
            <div style={{ fontSize: '14px', color: theme.colors.textSecondary, marginTop: '8px' }}>
              İhraç Numarası
            </div>
            <div style={{ fontWeight: 'bold' }}>
              {selection.ihracNo}
            </div>
          </>
        )}
      </div>

      {/* Status Cards */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <div style={{ 
          textAlign: 'center',
          padding: '12px',
          backgroundColor: theme.colors.success + '20',
          borderRadius: '6px',
          border: `1px solid ${theme.colors.success}`
        }}>
          <div style={{ color: theme.colors.success, fontWeight: 'bold' }}>
            ✅ API Bağlantısı
          </div>
          <div style={{ fontSize: '12px', color: theme.colors.text }}>
            Sistem Çevrimiçi
          </div>
        </div>
        
        <div style={{ 
          textAlign: 'center',
          padding: '12px',
          backgroundColor: theme.colors.primary + '20',
          borderRadius: '6px',
          border: `1px solid ${theme.colors.primary}`
        }}>
          <div style={{ color: theme.colors.primary, fontWeight: 'bold' }}>
            🎯 Dashboard Hazır
          </div>
          <div style={{ fontSize: '12px', color: theme.colors.text }}>
            Veri Görüntüleniyor
          </div>
        </div>
      </div>

      {/* Quick Stats */}
      <div style={{ marginTop: '20px' }}>
        <h5 style={{ color: theme.colors.text, marginBottom: '10px' }}>⚡ Hızlı İstatistikler</h5>
        <div style={{ fontSize: '12px', color: theme.colors.textSecondary }}>
          • Dashboard Sayısı: 5<br />
          • Aktif Filtreler: {[selection.kaynakKurulus, selection.fonNo, selection.ihracNo].filter(Boolean).length}<br />
          • Son Güncelleme: Az önce
        </div>
      </div>
    </div>
  );

  // Main Dashboard Content
  const renderMainContent = () => {
    if (!selection.kaynakKurulus) {
      return (
        <div style={{ 
          padding: '60px 40px', 
          textAlign: 'center',
          backgroundColor: theme.colors.surface,
          borderRadius: '12px',
          border: `2px dashed ${theme.colors.border}`,
          margin: '20px 0'
        }}>
          <div style={{ fontSize: '48px', marginBottom: '20px' }}>🎯</div>
          <h2 style={{ color: theme.colors.text, marginBottom: '15px' }}>
            Dashboard'a Hoş Geldiniz!
          </h2>
          <p style={{ color: theme.colors.textSecondary, fontSize: '16px', maxWidth: '500px', margin: '0 auto' }}>
            Analiz yapmaya başlamak için soldaki panelden <strong>Kaynak Kuruluş</strong> seçin.
          </p>
          
          <div style={{ 
            marginTop: '30px', 
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
            gap: '15px',
            maxWidth: '600px',
            margin: '30px auto 0'
          }}>
            <div style={{ 
              padding: '20px', 
              backgroundColor: theme.colors.background, 
              borderRadius: '8px',
              border: `1px solid ${theme.colors.border}`
            }}>
              <div style={{ fontSize: '24px', marginBottom: '8px' }}>📈</div>
              <div style={{ fontWeight: 'bold', color: theme.colors.text }}>5 Dashboard</div>
              <div style={{ fontSize: '12px', color: theme.colors.textSecondary }}>Farklı analiz türü</div>
            </div>
            
            <div style={{ 
              padding: '20px', 
              backgroundColor: theme.colors.background, 
              borderRadius: '8px',
              border: `1px solid ${theme.colors.border}`
            }}>
              <div style={{ fontSize: '24px', marginBottom: '8px' }}>🎛️</div>
              <div style={{ fontWeight: 'bold', color: theme.colors.text }}>Akıllı Filtreler</div>
              <div style={{ fontSize: '12px', color: theme.colors.textSecondary }}>Otomatik filtre sistemi</div>
            </div>
            
            <div style={{ 
              padding: '20px', 
              backgroundColor: theme.colors.background, 
              borderRadius: '8px',
              border: `1px solid ${theme.colors.border}`
            }}>
              <div style={{ fontSize: '24px', marginBottom: '8px' }}>📱</div>
              <div style={{ fontWeight: 'bold', color: theme.colors.text }}>Responsive</div>
              <div style={{ fontSize: '12px', color: theme.colors.textSecondary }}>Mobil uyumlu tasarım</div>
            </div>
          </div>
        </div>
      );
    }

    // Dashboard render logic
    if (selection.dashboardType === 'analysis' && !isFaizOraniValid) {
      return (
        <div style={{ 
          padding: '40px', 
          textAlign: 'center',
          backgroundColor: theme.colors.surface,
          borderRadius: '12px',
          border: `2px solid ${theme.colors.warning}`,
          margin: '20px 0'
        }}>
          <div style={{ fontSize: '48px', marginBottom: '20px' }}>⚠️</div>
          <h3 style={{ color: theme.colors.warning, marginBottom: '15px' }}>
            Faiz Oranı Gerekli!
          </h3>
          <p style={{ color: theme.colors.text }}>
            Nakit Akış Analizi için lütfen soldaki panelden <strong>faiz oranını</strong> girin.
          </p>
        </div>
      );
    }

    return (
      <DashboardContainer 
        dashboardType={selection.dashboardType}
        kaynakKurulus={selection.kaynakKurulus}
        fonNo={selection.fonNo}
        ihracNo={selection.ihracNo}
        faizOrani={selection.faizOrani || 15} // Default değer
      />
    );
  };

  return (
    <div className="App" style={{ 
      backgroundColor: theme.colors.background,
      color: theme.colors.text,
      minHeight: '100vh'
    }}>
      
      {/* Modern Navbar */}
      <Navbar 
        selectedDashboard={selection.dashboardType}
        onDashboardChange={handleDashboardChange}
      />
      
      {/* Layout Container */}
      <LayoutContainer
        sidebar={renderSidebar()}
        rightPanel={renderRightPanel()}
      >
        {renderMainContent()}
      </LayoutContainer>
    </div>
  );
}

export default App;