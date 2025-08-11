import React, { useState } from 'react';
import './App.css';
import HealthCheck from './components/Dashboard/HealthCheck';
import AdvancedVariableSelector from './components/Variables/AdvancedVariableSelector';
import DashboardContainer from './components/Dashboard/DashboardContainer';

interface SelectionState {
  dashboardType: string;
  kaynakKurulus: string;
  fonNo: string;
  ihracNo: string;
}

function App() {
  const [selection, setSelection] = useState<SelectionState>({
    dashboardType: 'trends',
    kaynakKurulus: '',
    fonNo: '',
    ihracNo: ''
  });

  const handleSelectionChange = React.useCallback((newSelection: SelectionState) => {
  console.log('Dashboard selection changed:', newSelection);
  setSelection(newSelection);
}, []); 

  return (
    <div className="App">
      <header style={{ 
        padding: '20px', 
        backgroundColor: '#2c3e50', 
        color: 'white',
        marginBottom: '20px',
        textAlign: 'center'
      }}>
        <h1>💰 Nakit Akış Dashboard</h1>
        <div style={{ fontSize: '14px', opacity: 0.8, marginTop: '10px' }}>
          🔗 API Status • 📊 Multiple Dashboards • 🎛️ Advanced Filtering
        </div>
      </header>
      
      <main style={{ padding: '20px', maxWidth: '1400px', margin: '0 auto' }}>
        {/* Health Check */}
        <HealthCheck />
        
        {/* Advanced Variable Selector */}
        <AdvancedVariableSelector onSelectionChange={handleSelectionChange} />
        
        {/* Dashboard Container */}
        {selection.kaynakKurulus ? (
          <DashboardContainer 
            dashboardType={selection.dashboardType}
            kaynakKurulus={selection.kaynakKurulus}
            fonNo={selection.fonNo}
            ihracNo={selection.ihracNo}
          />
        ) : (
          <div style={{ 
            padding: '40px', 
            textAlign: 'center',
            border: '2px dashed #ddd',
            borderRadius: '8px',
            backgroundColor: '#f8f9fa',
            marginTop: '20px'
          }}>
            <h3>🎯 Dashboard Hazır!</h3>
            <p>Lütfen yukarıdan <strong>Kaynak Kuruluş</strong> seçin.</p>
            <div style={{ marginTop: '20px', fontSize: '14px', color: '#666' }}>
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
          backgroundColor: '#f8f9fa', 
          borderRadius: '8px',
          textAlign: 'center'
        }}>
          <h3>🎯 Dashboard Features</h3>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
            gap: '15px',
            marginTop: '15px'
          }}>
            <div style={{ padding: '15px', backgroundColor: 'white', borderRadius: '6px' }}>
              <strong>✅ Completed</strong><br />
              • React App Setup<br />
              • API Integration<br />
              • Advanced Variable Selection<br />
              • Multi-Dashboard Support<br />
              • Trend Analytics
            </div>
            <div style={{ padding: '15px', backgroundColor: 'white', borderRadius: '6px' }}>
              <strong>🎛️ Active Features</strong><br />
              • Kaynak Kuruluş Filter<br />
              • Fon No Filter<br />
              • İhraç No Filter<br />
              • Dashboard Type Selection<br />
              • Real-time Data Loading
            </div>
            <div style={{ padding: '15px', backgroundColor: 'white', borderRadius: '6px' }}>
              <strong>🚀 Ready for Production</strong><br />
              • Docker Containerization<br />
              • Grafana Integration<br />
              • API Health Monitoring<br />
              • Error Handling<br />
              • Responsive Design
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;