import React, { useState } from 'react';
import './App.css';
import HealthCheck from './components/Dashboard/HealthCheck';
import AdvancedVariableSelector from './components/Variables/AdvancedVariableSelector';
import DashboardContainer from './components/Dashboard/DashboardContainer';
import ThemeToggle from './components/UI/ThemeToggle';
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

  // FAİZ ORANI KONTROLÜ
  const isFaizOraniValid = selection.faizOrani !== null && selection.faizOrani > 0;

  return (
    <div 
      className="App" 
      style={{ 
        backgroundColor: theme.colors.background,
        color: theme.colors.text,
        minHeight: '100vh'
      }}
    >
      <header style={{ 
        padding: '20px', 
        backgroundColor: theme.colors.surface,
        color: theme.colors.text,
        marginBottom: '20px',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        borderBottom: `1px solid ${theme.colors.border}`
      }}>
        <div style={{ textAlign: 'center', flex: 1 }}>
          <h1>💰 Nakit Akış Dashboard Suite</h1>
          <p style={{ color: theme.colors.textSecondary }}>
            React + .NET API + Multi-Dashboard Analytics
          </p>
          <div style={{ 
            fontSize: '14px', 
            opacity: 0.8, 
            marginTop: '10px',
            color: theme.colors.textSecondary
          }}>
            🔗 API Status • 📊 Multiple Dashboards • 🎛️ Advanced Filtering
          </div>
        </div>
        
        <div style={{ position: 'absolute', top: '20px', right: '20px' }}>
          <ThemeToggle />
        </div>
      </header>
      
      <main style={{ padding: '20px', maxWidth: '1400px', margin: '0 auto' }}>
        <HealthCheck />
        
        <AdvancedVariableSelector onSelectionChange={handleSelectionChange} />
        
        {/* DASHBOARD RENDER KONTROLLÜ - SADECE ANALYSIS İÇİN FAİZ KONTROLÜ */}
        {selection.kaynakKurulus ? (
          selection.dashboardType === 'analysis' ? (
            // Analysis dashboard için faiz oranı kontrolü
            isFaizOraniValid ? (
              <DashboardContainer 
                dashboardType={selection.dashboardType}
                kaynakKurulus={selection.kaynakKurulus}
                fonNo={selection.fonNo}
                ihracNo={selection.ihracNo}
                faizOrani={selection.faizOrani!}
              />
            ) : (
              <div style={{ 
                padding: '40px', 
                textAlign: 'center',
                border: `2px dashed ${theme.colors.error}`,
                borderRadius: '8px',
                backgroundColor: theme.colors.surface,
                marginTop: '20px'
              }}>
                <h3 style={{ color: theme.colors.error }}>⚠️ Faiz Oranı Gerekli!</h3>
                <p style={{ color: theme.colors.text }}>
                  Nakit Akış Analizi için lütfen <strong>faiz oranını</strong> girin.
                </p>
                <div style={{ 
                  marginTop: '15px', 
                  fontSize: '14px', 
                  color: theme.colors.textSecondary 
                }}>
                  📊 Faiz oranı: 0.1 - 100 arası değer giriniz
                </div>
              </div>
            )
          ) : (
            // Diğer dashboard'lar için faiz oranı kontrolü yok
            <DashboardContainer 
              dashboardType={selection.dashboardType}
              kaynakKurulus={selection.kaynakKurulus}
              fonNo={selection.fonNo}
              ihracNo={selection.ihracNo}
              faizOrani={15} // Default değer, kullanılmayacak
            />
          )
        ) : (
          // Kuruluş seçilmemiş durumu
          <div style={{ 
            padding: '40px', 
            textAlign: 'center',
            border: `2px dashed ${theme.colors.border}`,
            borderRadius: '8px',
            backgroundColor: theme.colors.surface,
            marginTop: '20px'
          }}>
            <h3 style={{ color: theme.colors.text }}>🎯 Dashboard Hazır!</h3>
            <p style={{ color: theme.colors.textSecondary }}>
              Lütfen yukarıdan <strong>Kaynak Kuruluş</strong> seçin.
            </p>
            <div style={{ 
              marginTop: '20px', 
              fontSize: '14px', 
              color: theme.colors.textSecondary 
            }}>
              <strong>Kullanılabilir Dashboard'lar:</strong><br />
              📈 Haftalık Trend Analizi<br />
              💰 Nakit Akış Analizi<br />
              📊 Geçmiş Veriler<br />
              ⚖️ Kuruluş Karşılaştırma
            </div>
          </div>
        )}
        
        {/* Footer Info */}
        <div style={{ 
          marginTop: '40px', 
          padding: '20px', 
          backgroundColor: theme.colors.surface,
          borderRadius: '8px',
          textAlign: 'center',
          border: `1px solid ${theme.colors.border}`
        }}>
          <h3 style={{ color: theme.colors.text }}>🎯 Dashboard Features</h3>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
            gap: '15px',
            marginTop: '15px'
          }}>
            <div style={{ 
              padding: '15px', 
              backgroundColor: theme.colors.background, 
              borderRadius: '6px',
              border: `1px solid ${theme.colors.border}`
            }}>
              <strong style={{ color: theme.colors.success }}>✅ Completed</strong><br />
              <span style={{ color: theme.colors.text }}>
                • React App Setup<br />
                • API Integration<br />
                • Advanced Variable Selection<br />
                • Multi-Dashboard Support<br />
                • Trend Analytics
              </span>
            </div>
            <div style={{ 
              padding: '15px', 
              backgroundColor: theme.colors.background, 
              borderRadius: '6px',
              border: `1px solid ${theme.colors.border}`
            }}>
              <strong style={{ color: theme.colors.primary }}>🎛️ Active Features</strong><br />
              <span style={{ color: theme.colors.text }}>
                • Kaynak Kuruluş Filter<br />
                • Fon No Filter<br />
                • İhraç No Filter<br />
                • Dashboard Type Selection<br />
                • Real-time Data Loading
              </span>
            </div>
            <div style={{ 
              padding: '15px', 
              backgroundColor: theme.colors.background, 
              borderRadius: '6px',
              border: `1px solid ${theme.colors.border}`
            }}>
              <strong style={{ color: theme.colors.warning }}>🚀 Ready for Production</strong><br />
              <span style={{ color: theme.colors.text }}>
                • Docker Containerization<br />
                • Grafana Integration<br />
                • API Health Monitoring<br />
                • Error Handling<br />
                • Responsive Design
              </span>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;