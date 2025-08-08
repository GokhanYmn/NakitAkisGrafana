import React, { useState, useEffect } from 'react';
import NakitAkisApi from '../../services/nakitAkisApi';

interface HealthStatus {
  status: string;
  timestamp: string;
  isLoading: boolean;
  error?: string;
}

const HealthCheck: React.FC = () => {
  const [health, setHealth] = useState<HealthStatus>({
    status: '',
    timestamp: '',
    isLoading: true
  });

  const checkHealth = async () => {
    try {
      setHealth(prev => ({ ...prev, isLoading: true, error: undefined }));
      const result = await NakitAkisApi.healthCheck();
      setHealth({
        status: result.status,
        timestamp: result.timestamp,
        isLoading: false
      });
    } catch (error) {
      setHealth({
        status: 'error',
        timestamp: new Date().toISOString(),
        isLoading: false,
        error: error instanceof Error ? error.message : 'API bağlantı hatası'
      });
    }
  };

  useEffect(() => {
    checkHealth();
  }, []);

  return (
    <div style={{ marginBottom: '20px', padding: '15px', border: '1px solid #ddd', borderRadius: '8px' }}>
      <h3>🔍 API Health Check</h3>
      
      {health.isLoading && (
        <div style={{ color: 'blue' }}>
          ⏳ API kontrol ediliyor...
        </div>
      )}

      {!health.isLoading && health.status === 'healthy' && (
        <div style={{ color: 'green' }}>
          ✅ API çalışıyor! Son kontrol: {new Date(health.timestamp).toLocaleString('tr-TR')}
        </div>
      )}

      {!health.isLoading && health.status === 'error' && (
        <div style={{ color: 'red' }}>
          ❌ API Hatası: {health.error}
        </div>
      )}

      <button 
        onClick={checkHealth} 
        disabled={health.isLoading}
        style={{ marginTop: '10px', padding: '8px 16px' }}
      >
        🔄 Tekrar Kontrol Et
      </button>
    </div>
  );
};

export default HealthCheck;