import React from 'react';
import { useTheme } from '../../contexts/ThemeContext';
import ThemeToggle from '../UI/ThemeToggle';
import { DASHBOARD_TYPES, DashboardConfig } from '../../types/nakitAkis';

interface NavbarProps {
  selectedDashboard: string;
  onDashboardChange: (dashboardId: string) => void;
}

const Navbar: React.FC<NavbarProps> = ({ selectedDashboard, onDashboardChange }) => {
  const { theme } = useTheme();

  return (
    <nav style={{
      position: 'sticky',
      top: 0,
      zIndex: 1000,
      background: `linear-gradient(135deg, ${theme.colors.primary} 0%, #764ba2 100%)`,
      color: 'white',
      padding: '0',
      boxShadow: '0 2px 20px rgba(0,0,0,0.1)',
      borderBottom: `1px solid ${theme.colors.border}`
    }}>
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        maxWidth: '1400px',
        margin: '0 auto',
        padding: '0 20px',
        height: '70px'
      }}>
        
        {/* Logo & Brand */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '15px'
        }}>
          <div style={{
            width: '45px',
            height: '45px',
            borderRadius: '12px',
            background: 'rgba(255,255,255,0.2)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '22px',
            fontWeight: 'bold',
            backdropFilter: 'blur(10px)'
          }}>
            💰
          </div>
          <div>
            <h1 style={{ 
              margin: 0, 
              fontSize: '22px', 
              fontWeight: '700',
              letterSpacing: '-0.5px'
            }}>
              NakitAkış Analytics
            </h1>
            <div style={{ 
              fontSize: '12px', 
              opacity: 0.9,
              fontWeight: '500'
            }}>
              Professional Dashboard Suite
            </div>
          </div>
        </div>

        {/* Dashboard Tabs */}
        <div style={{
          display: 'flex',
          gap: '8px',
          background: 'rgba(255,255,255,0.15)',
          borderRadius: '12px',
          padding: '6px',
          backdropFilter: 'blur(10px)'
        }}>
          {DASHBOARD_TYPES.map((dashboard: DashboardConfig) => (
            <button
              key={dashboard.id}
              onClick={() => onDashboardChange(dashboard.id)}
              style={{
                background: selectedDashboard === dashboard.id 
                  ? 'rgba(255,255,255,0.25)' 
                  : 'transparent',
                color: 'white',
                border: 'none',
                borderRadius: '8px',
                padding: '10px 16px',
                fontSize: '14px',
                fontWeight: selectedDashboard === dashboard.id ? '600' : '500',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                transition: 'all 0.3s ease',
                backdropFilter: selectedDashboard === dashboard.id ? 'blur(20px)' : 'none',
                boxShadow: selectedDashboard === dashboard.id ? '0 2px 10px rgba(0,0,0,0.2)' : 'none'
              }}
              onMouseOver={(e) => {
                if (selectedDashboard !== dashboard.id) {
                  e.currentTarget.style.background = 'rgba(255,255,255,0.1)';
                }
              }}
              onMouseOut={(e) => {
                if (selectedDashboard !== dashboard.id) {
                  e.currentTarget.style.background = 'transparent';
                }
              }}
            >
              <span style={{ fontSize: '16px' }}>{dashboard.icon}</span>
              <span style={{ 
                whiteSpace: 'nowrap',
                display: window.innerWidth < 768 ? 'none' : 'block' // Mobile'da text gizle
              }}>
                {dashboard.name.split(' ')[0]} {/* İlk kelimeyi göster */}
              </span>
            </button>
          ))}
        </div>

        {/* Right Actions */}
        <div style={{
          display: 'flex',
          alignItems: 'center',
          gap: '15px'
        }}>
          {/* Status Indicator */}
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            background: 'rgba(255,255,255,0.15)',
            borderRadius: '20px',
            padding: '8px 12px',
            fontSize: '12px',
            fontWeight: '500'
          }}>
            <div style={{
              width: '8px',
              height: '8px',
              borderRadius: '50%',
              background: '#10b981',
              boxShadow: '0 0 10px #10b981'
            }}></div>
            <span style={{ display: window.innerWidth < 768 ? 'none' : 'block' }}>
              API Online
            </span>
          </div>

          {/* Theme Toggle */}
          <ThemeToggle />

          {/* User Menu Placeholder */}
          <button style={{
            width: '40px',
            height: '40px',
            borderRadius: '50%',
            background: 'rgba(255,255,255,0.2)',
            border: 'none',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '18px',
            cursor: 'pointer',
            transition: 'all 0.3s ease'
          }}>
            👤
          </button>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;