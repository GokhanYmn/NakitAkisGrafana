import React from 'react';
import { useTheme } from '../../contexts/ThemeContext';

const ThemeToggle: React.FC = () => {
  const { isDark, toggleTheme } = useTheme();

  return (
    <button
      onClick={toggleTheme}
      style={{
        background: 'rgba(0, 0, 0, 0.15)',
        color: 'white',
        border: 'none',
        borderRadius: '20px',
        cursor: 'pointer',
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        fontSize: '14px',
        fontWeight: '500',
        transition: 'all 0.3s ease',
        padding: '8px 12px',
        backdropFilter: 'blur(10px)'
      }}
      onMouseOver={(e) => {
        e.currentTarget.style.background = 'rgba(0, 0, 0, 0.25)';
      }}
      onMouseOut={(e) => {
        e.currentTarget.style.background = 'rgba(0, 0, 0, 0.15)';
      }}
    >
      <span style={{ fontSize: '16px' }}>
        {isDark ? '☀️' : '🌙'}
      </span>
      <span style={{ 
        display: window.innerWidth < 768 ? 'none' : 'block' 
      }}>
        {isDark ? 'Light' : 'Dark'}
      </span>
    </button>
  );
};

export default ThemeToggle;