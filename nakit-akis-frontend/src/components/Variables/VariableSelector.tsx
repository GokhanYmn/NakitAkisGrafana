import React,{useState,useEffect} from 'react';
import NakitAkisApi from '../../services/nakitAkisApi';
import { GrafanaVariable } from '../../types/nakitAkis';

interface VariableSelectorProps {
  onKurulusChange: (kurulus: string) => void;
}

const VariableSelector: React.FC<VariableSelectorProps> = ({ onKurulusChange }) => {
  const [kaynakKuruluslar, setKaynakKuruluslar] = useState<GrafanaVariable[]>([]);
  const [selectedKurulus, setSelectedKurulus] = useState<string>('');
  const [loading, setLoading] = useState(false);

 useEffect(() => {
  const loadKuruluslar = async () => {
    try {
      setLoading(true);
      const data = await NakitAkisApi.getKaynakKuruluslar();
      console.log('Kuruluşlar yüklendi:', data);
      setKaynakKuruluslar(data);
      
      // İlk kuruluşu default seç - SADECE 1 KERE
      if (data.length > 0 && !selectedKurulus) {
        const firstKurulus = data[0].value;
        console.log('Default kuruluş seçiliyor:', firstKurulus);
        setSelectedKurulus(firstKurulus);
        onKurulusChange(firstKurulus);
      }
    } catch (error) {
      console.error('Kuruluş yükleme hatası:', error);
    } finally {
      setLoading(false);
    }
  };

  loadKuruluslar();
}, []);

  const handleKurulusChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const kurulus = event.target.value;
    console.log('Dropdown değişti:',kurulus);//debug
    setSelectedKurulus(kurulus);
    onKurulusChange(kurulus);
  };

  return (
    <div style={{ marginBottom: '20px', padding: '15px', border: '1px solid #ddd', borderRadius: '8px' }}>
      <h3>🏢 Kaynak Kuruluş Seç</h3>
      
      {loading && <div style={{ color: 'blue' }}>⏳ Kuruluşlar yükleniyor...</div>}
      
      <select 
        value={selectedKurulus}
        onChange={handleKurulusChange}
        disabled={loading}
        style={{ 
          padding: '8px 12px', 
          fontSize: '16px', 
          width: '300px',
          marginTop: '10px'
        }}
      >
        <option value="">Seçiniz...</option>
        {kaynakKuruluslar.map((kurulus) => (
          <option key={kurulus.value} value={kurulus.value}>
            {kurulus.text}
          </option>
        ))}
      </select>
      
      {selectedKurulus && (
        <div style={{ marginTop: '10px', color: 'green' }}>
          ✅ Seçili: <strong>{selectedKurulus}</strong>
        </div>
      )}
    </div>
  );
};

export default VariableSelector;