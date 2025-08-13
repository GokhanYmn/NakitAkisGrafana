import React, { useState, useEffect } from 'react';
import NakitAkisApi from '../../services/nakitAkisApi';
import { GrafanaVariable, DASHBOARD_TYPES, DashboardConfig } from '../../types/nakitAkis';
import { useTheme } from '../../contexts/ThemeContext';
import ExportMenu from '../UI/ExportMenu';

interface AdvancedVariableSelectorProps {
  onSelectionChange: (selection: {
    dashboardType: string;
    kaynakKurulus: string;
    fonNo: string;
    ihracNo: string;
    faizOrani: number | null;
  }) => void;
}

const AdvancedVariableSelector: React.FC<AdvancedVariableSelectorProps> = ({ onSelectionChange }) => {
  const { theme } = useTheme();
  
  // State'ler
  const [dashboardType, setDashboardType] = useState<string>('trends');
  const [kaynakKuruluslar, setKaynakKuruluslar] = useState<GrafanaVariable[]>([]);
  const [fonlar, setFonlar] = useState<GrafanaVariable[]>([]);
  const [ihraclar, setIhraclar] = useState<GrafanaVariable[]>([]);
  
  const [selectedKurulus, setSelectedKurulus] = useState<string>('');
  const [selectedFon, setSelectedFon] = useState<string>('');
  const [selectedIhrac, setSelectedIhrac] = useState<string>('');
  
  // FAİZ ORANI BOŞ BAŞLIYOR
  const [currentFaizOrani, setCurrentFaizOrani] = useState<number | null>(null);
  const [faizOraniInput, setFaizOraniInput] = useState<string>('');
  
  const [loading, setLoading] = useState({
    kurulus: false,
    fon: false,
    ihrac: false
  });

  const [analysisData, setAnalysisData] = useState<any>(null);

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
          setAnalysisData(null);
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
          setAnalysisData(null);
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

  // FAİZ ORANI CHANGE HANDLER
  const handleFaizOraniChange = (value: string) => {
    setFaizOraniInput(value);
    const numValue = parseFloat(value);
    if (!isNaN(numValue) && numValue > 0 && numValue <= 100) {
      setCurrentFaizOrani(numValue);
    } else {
      setCurrentFaizOrani(null);
    }
  };

  // Seçim değişikliklerini parent'a bildir
  useEffect(() => {
    onSelectionChange({
      dashboardType,
      kaynakKurulus: selectedKurulus,
      fonNo: selectedFon,
      ihracNo: selectedIhrac,
      faizOrani: currentFaizOrani
    });
  }, [dashboardType, selectedKurulus, selectedFon, selectedIhrac, currentFaizOrani, onSelectionChange]);

  // Quick Analysis fonksiyonu
  const handleQuickAnalysis = async () => {
    if (!selectedKurulus || !currentFaizOrani) return;
    
    try {
      const quickData = await NakitAkisApi.getAnalysis({
        faizOrani: currentFaizOrani,
        kaynakKurulus: selectedKurulus,
        fonNo: selectedFon,
        ihracNo: selectedIhrac
      });
      setAnalysisData(quickData);
      console.log('Quick analysis completed for export:', quickData);
    } catch (error) {
      console.error('Quick analysis failed:', error);
    }
  };

  const selectStyles = {
    padding: '10px',
    fontSize: '14px',
    borderRadius: '6px',
    border: `1px solid ${theme.colors.border}`,
    backgroundColor: theme.colors.background,
    color: theme.colors.text,
    minWidth: '200px'
  };

  // FAİZ ORANI VALIDATION
  const isFaizOraniValid = currentFaizOrani !== null && currentFaizOrani > 0;

  return (
    <div style={{ 
      padding: '20px', 
      border: `1px solid ${theme.colors.border}`,
      borderRadius: '8px',
      backgroundColor: theme.colors.surface,
      marginBottom: '20px'
    }}>
      <h3 style={{ color: theme.colors.text }}>🎛️ Dashboard & Filtre Seçimi</h3>
      
      {/* Dashboard Type Seçimi */}
      <div style={{ marginBottom: '15px' }}>
        <label style={{ 
          display: 'block', 
          marginBottom: '5px', 
          fontWeight: 'bold',
          color: theme.colors.text
        }}>
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
        <div style={{ 
          fontSize: '12px', 
          color: theme.colors.textSecondary,
          marginTop: '5px' 
        }}>
          {DASHBOARD_TYPES.find(d => d.id === dashboardType)?.description}
        </div>
      </div>

      {/* Variable Seçimleri */}
      <div style={{ display: 'flex', gap: '15px', justifyContent:'center', flexWrap: 'wrap', alignItems: 'center' }}>
        {/* Kaynak Kuruluş */}
        <div>
          <label style={{ 
            display: 'block',            
            marginBottom: '5px', 
            fontWeight: 'bold',
            color: theme.colors.text
          }}>
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
          {loading.kurulus && (
            <div style={{ fontSize: '12px', color: theme.colors.primary }}>⏳ Yükleniyor...</div>
          )}
        </div>

        {/* Fon Numarası */}
        <div>
          <label style={{ 
            display: 'block', 
            marginBottom: '5px', 
            fontWeight: 'bold',
            color: theme.colors.text
          }}>
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
              <option key={`fon-${fon.value}`} value={fon.value}>
                {fon.text}
              </option>
            ))}
          </select>
          {loading.fon && (
            <div style={{ fontSize: '12px', color: theme.colors.primary }}>⏳ Yükleniyor...</div>
          )}
        </div>

        {/* İhraç Numarası */}
        <div>
          <label style={{ 
            display: 'block', 
            marginBottom: '5px', 
            fontWeight: 'bold',
            color: theme.colors.text
          }}>
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
          {loading.ihrac && (
            <div style={{ fontSize: '12px', color: theme.colors.primary }}>⏳ Yükleniyor...</div>
          )}
        </div>

        {/* FAİZ ORANI INPUT'U - SADECE ANALYSIS DASHBOARD'DA */}
        {selectedKurulus && dashboardType === 'analysis' && (
          <div>
            <label style={{ 
              display: 'block', 
              marginBottom: '5px', 
              fontWeight: 'bold',
              color: theme.colors.text
            }}>
              📊 Faiz Oranı (%): *
            </label>
            <input
              type="number"
              value={faizOraniInput}
              onChange={(e) => handleFaizOraniChange(e.target.value)}
              placeholder="Örn: 15.5"
              min="0.1"
              max="100"
              step="0.1"
              style={{
                padding: '10px',
                fontSize: '14px',
                borderRadius: '6px',
                border: `1px solid ${!isFaizOraniValid && faizOraniInput ? theme.colors.error : theme.colors.border}`,
                backgroundColor: theme.colors.background,
                color: theme.colors.text,
                width: '120px'
              }}
            />
            {!isFaizOraniValid && faizOraniInput && (
              <div style={{ fontSize: '12px', color: theme.colors.error, marginTop: '2px' }}>
                ⚠️ 0.1-100 arası değer girin
              </div>
            )}
          </div>
        )}

        {/* EXPORT MENU - SADECE ANALYSIS + FAİZ ORANI VALİD */}
        {selectedKurulus && dashboardType === 'analysis' && isFaizOraniValid && (
          <div>
            <label style={{ 
              display: 'block', 
              marginBottom: '5px', 
              fontWeight: 'bold',
              color: theme.colors.text
            }}>
              📥 Analizi Yazdır:
            </label>
            <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
              {!analysisData && (
                <button
                  onClick={handleQuickAnalysis}
                  style={{
                    padding: '10px 12px',
                    backgroundColor: theme.colors.warning,
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '12px'
                  }}
                >
                  📊 Analiz
                </button>
              )}
              
              {analysisData && (
                <ExportMenu 
                  analysisData={analysisData}
                  filters={{
                    kaynakKurulus: selectedKurulus,
                    fonNo: selectedFon,
                    ihracNo: selectedIhrac,
                    faizOrani: currentFaizOrani!
                  }}
                />
              )}
            </div>
          </div>
        )}
      </div>

      {/* Seçim Özeti - FAİZ ORANI SADECE ANALYSIS'DE */}
      {selectedKurulus && (
        <div style={{ 
          marginTop: '15px', 
          padding: '10px', 
          backgroundColor: theme.colors.background,
          borderRadius: '6px',
          fontSize: '14px',
          border: `1px solid ${theme.colors.border}`
        }}>
          <strong style={{ color: theme.colors.text }}>📋 Aktif Filtreler:</strong>
          <br />
          <span style={{ color: theme.colors.text }}>🏢 Kuruluş: <strong>{selectedKurulus}</strong></span>
          {selectedFon && (
            <>
              <br />
              <span style={{ color: theme.colors.text }}>💼 Fon: <strong>{selectedFon}</strong></span>
            </>
          )}
          {selectedIhrac && (
            <>
              <br />
              <span style={{ color: theme.colors.text }}>🎯 İhraç: <strong>{selectedIhrac}</strong></span>
            </>
          )}
          {/* FAİZ ORANI SADECE ANALYSIS DASHBOARD'DA */}
          {dashboardType === 'analysis' && (
            <>
              <br />
              {isFaizOraniValid ? (
                <span style={{ color: theme.colors.success }}>📊 Faiz Oranı: <strong>%{currentFaizOrani}</strong> ✅</span>
              ) : (
                <span style={{ color: theme.colors.error }}>📊 Faiz Oranı: <strong>Gerekli</strong> ⚠️</span>
              )}
            </>
          )}
          {analysisData && (
            <>
              <br />
              <span style={{ color: theme.colors.success, fontSize: '12px' }}>
                ✅ Export için analiz verileri hazır
              </span>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default AdvancedVariableSelector;