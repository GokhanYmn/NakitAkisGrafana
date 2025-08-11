import React, { useState, useEffect } from 'react';
import NakitAkisApi from '../../services/nakitAkisApi';
import { GrafanaVariable, DASHBOARD_TYPES, DashboardConfig } from '../../types/nakitAkis';

interface AdvancedVariableSelectorProps {
  onSelectionChange: (selection: {
    dashboardType: string;
    kaynakKurulus: string;
    fonNo: string;
    ihracNo: string;
  }) => void;
}

const AdvancedVariableSelector: React.FC<AdvancedVariableSelectorProps> = ({ onSelectionChange }) => {
  // State'ler
  const [dashboardType, setDashboardType] = useState<string>('trends');
  const [kaynakKuruluslar, setKaynakKuruluslar] = useState<GrafanaVariable[]>([]);
  const [fonlar, setFonlar] = useState<GrafanaVariable[]>([]);
  const [ihraclar, setIhraclar] = useState<GrafanaVariable[]>([]);
  
  const [selectedKurulus, setSelectedKurulus] = useState<string>('');
  const [selectedFon, setSelectedFon] = useState<string>('');
  const [selectedIhrac, setSelectedIhrac] = useState<string>('');
  
  const [loading, setLoading] = useState({
    kurulus: false,
    fon: false,
    ihrac: false
  });

  // Kaynak kuruluşları yükle
  useEffect(() => {
    const loadKuruluslar = async () => {
      try {
        setLoading(prev => ({ ...prev, kurulus: true }));
        const data = await NakitAkisApi.getKaynakKuruluslar();
        setKaynakKuruluslar(data);
        
        if (data.length > 0 && !selectedKurulus) {
          setSelectedKurulus(data[0].value);
        }
      } catch (error) {
        console.error('Kuruluş yükleme hatası:', error);
      } finally {
        setLoading(prev => ({ ...prev, kurulus: false }));
      }
    };

    loadKuruluslar();
  }, []);

  // Kuruluş değiştiğinde fonları yükle
  useEffect(() => {
    if (selectedKurulus) {
      const loadFonlar = async () => {
        try {
          setLoading(prev => ({ ...prev, fon: true }));
          const data = await NakitAkisApi.getFonlar(selectedKurulus);
          setFonlar(data);
          setSelectedFon('');
          setSelectedIhrac('');
          setIhraclar([]);
        } catch (error) {
          console.error('Fon yükleme hatası:', error);
          setFonlar([]);
        } finally {
          setLoading(prev => ({ ...prev, fon: false }));
        }
      };

      loadFonlar();
    }
  }, [selectedKurulus]);

  // Fon değiştiğinde ihraçları yükle
  useEffect(() => {
    if (selectedKurulus && selectedFon) {
      const loadIhraclar = async () => {
        try {
          setLoading(prev => ({ ...prev, ihrac: true }));
          const data = await NakitAkisApi.getIhraclar(selectedKurulus, selectedFon);
          setIhraclar(data);
          setSelectedIhrac('');
        } catch (error) {
          console.error('İhraç yükleme hatası:', error);
          setIhraclar([]);
        } finally {
          setLoading(prev => ({ ...prev, ihrac: false }));
        }
      };

      loadIhraclar();
    }
  }, [selectedKurulus, selectedFon]);

  // Seçim değişikliklerini parent'a bildir
useEffect(() => {
  onSelectionChange({
    dashboardType,
    kaynakKurulus: selectedKurulus,
    fonNo: selectedFon,
    ihracNo: selectedIhrac
  });
}, [dashboardType, selectedKurulus, selectedFon, selectedIhrac]);

  const selectStyles = {
    padding: '10px',
    fontSize: '14px',
    borderRadius: '6px',
    border: '1px solid #ddd',
    backgroundColor: 'white',
    minWidth: '200px'
  };

  return (
    <div style={{ 
      padding: '20px', 
      border: '1px solid #ddd', 
      borderRadius: '8px',
      backgroundColor: '#f9f9f9',
      marginBottom: '20px'
    }}>
      <h3>🎛️ Dashboard & Filtre Seçimi</h3>
      
      {/* Dashboard Type Seçimi */}
      <div style={{ marginBottom: '15px' }}>
        <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>
          📊 Dashboard Türü:
        </label>
        <select 
          value={dashboardType}
          onChange={(e) => setDashboardType(e.target.value)}
          style={selectStyles}
        >
          {DASHBOARD_TYPES.map((dashboard: DashboardConfig) => (
            <option key={dashboard.id} value={dashboard.id}>
              {dashboard.icon} {dashboard.name}
            </option>
          ))}
        </select>
        <div style={{ fontSize: '12px', color: '#666', marginTop: '5px' }}>
          {DASHBOARD_TYPES.find(d => d.id === dashboardType)?.description}
        </div>
      </div>

      {/* Variable Seçimleri */}
      <div style={{ display: 'flex', gap: '15px', flexWrap: 'wrap', alignItems: 'end' }}>
        {/* Kaynak Kuruluş */}
        <div>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>
            🏢 Kaynak Kuruluş: *
          </label>
          <select 
            value={selectedKurulus}
            onChange={(e) => setSelectedKurulus(e.target.value)}
            disabled={loading.kurulus}
            style={selectStyles}
          >
            <option value="">Seçiniz...</option>
            {kaynakKuruluslar.map((kurulus) => (
              <option key={kurulus.value} value={kurulus.value}>
                {kurulus.text}
              </option>
            ))}
          </select>
          {loading.kurulus && <div style={{ fontSize: '12px', color: 'blue' }}>⏳ Yükleniyor...</div>}
        </div>

        {/* Fon Numarası */}
        <div>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>
            💼 Fon Numarası:
          </label>
          <select 
            value={selectedFon}
            onChange={(e) => setSelectedFon(e.target.value)}
            disabled={loading.fon || !selectedKurulus}
            style={selectStyles}
          >
            <option value="">Tüm Fonlar</option>
            {fonlar.map((fon) => (
              <option key={fon.value} value={fon.value}>
                {fon.text}
              </option>
            ))}
          </select>
          {loading.fon && <div style={{ fontSize: '12px', color: 'blue' }}>⏳ Yükleniyor...</div>}
        </div>

        {/* İhraç Numarası */}
        <div>
          <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>
    🎯 İhraç Numarası:
         </label>
          <select 
    value={selectedIhrac}
    onChange={(e) => setSelectedIhrac(e.target.value)}
    disabled={loading.ihrac || !selectedFon}
    style={selectStyles}
  >
    <option value="">Tüm İhraçlar</option>
    {ihraclar.map((ihrac, index) => (
      <option key={`ihrac-${ihrac.value}-${index}`} value={ihrac.value}>
        {ihrac.text}
      </option>
    ))}
  </select>
  {loading.ihrac && <div style={{ fontSize: '12px', color: 'blue' }}>⏳ Yükleniyor...</div>}
        </div>
      </div>

      {/* Seçim Özeti */}
      {selectedKurulus && (
        <div style={{ 
          marginTop: '15px', 
          padding: '10px', 
          backgroundColor: '#e8f5e8', 
          borderRadius: '6px',
          fontSize: '14px'
        }}>
          <strong>📋 Aktif Filtreler:</strong>
          <br />
          🏢 Kuruluş: <strong>{selectedKurulus}</strong>
          {selectedFon && <><br />💼 Fon: <strong>{selectedFon}</strong></>}
          {selectedIhrac && <><br />🎯 İhraç: <strong>{selectedIhrac}</strong></>}
        </div>
      )}
    </div>
  );
};

export default AdvancedVariableSelector;