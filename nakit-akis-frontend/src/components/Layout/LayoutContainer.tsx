import React, { useState } from 'react';
import { useTheme } from '../../contexts/ThemeContext';

interface LayoutContainerProps {
  sidebar: React.ReactNode;
  children: React.ReactNode;
  rightPanel?: React.ReactNode;
}

const LayoutContainer: React.FC<LayoutContainerProps> = ({ 
  sidebar, 
  children, 
  rightPanel 
}) => {
  const { theme } = useTheme();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [sidebarVisible, setSidebarVisible] = useState(true);

  // Mobile detection
  const isMobile = window.innerWidth < 768;

  return (
    <div style={{
      display: 'flex',
      minHeight: 'calc(100vh - 70px)', // Navbar height çıkar
      backgroundColor: theme.colors.background
    }}>
      
      {/* Sidebar */}
      {sidebarVisible && (
        <aside style={{
          width: sidebarCollapsed ? '80px' : isMobile ? '100%' : '320px',
          backgroundColor: theme.colors.surface,
          borderRight: `1px solid ${theme.colors.border}`,
          transition: 'all 0.3s ease',
          position: isMobile ? 'fixed' : 'relative',
          top: isMobile ? '70px' : '0',
          left: isMobile ? '0' : 'auto',
          height: isMobile ? 'calc(100vh - 70px)' : 'auto',
          zIndex: isMobile ? 999 : 'auto',
          boxShadow: isMobile ? '2px 0 10px rgba(0,0,0,0.1)' : 'none',
          overflowY: 'auto'
        }}>
          
          {/* Sidebar Header */}
          <div style={{
            padding: '20px',
            borderBottom: `1px solid ${theme.colors.border}`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between'
          }}>
            {!sidebarCollapsed && (
              <h3 style={{ 
                margin: 0, 
                color: theme.colors.text,
                fontSize: '18px',
                fontWeight: '600'
              }}>
                🎛️ Filtreler & Ayarlar
              </h3>
            )}
            
            <div style={{ display: 'flex', gap: '10px' }}>
              {/* Collapse Toggle */}
              {!isMobile && (
                <button
                  onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
                  style={{
                    background: 'none',
                    border: `1px solid ${theme.colors.border}`,
                    borderRadius: '6px',
                    width: '32px',
                    height: '32px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    cursor: 'pointer',
                    color: theme.colors.text,
                    transition: 'all 0.2s ease'
                  }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.backgroundColor = theme.colors.primary;
                    e.currentTarget.style.color = 'white';
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.backgroundColor = 'transparent';
                    e.currentTarget.style.color = theme.colors.text;
                  }}
                >
                  {sidebarCollapsed ? '▶️' : '◀️'}
                </button>
              )}
              
              {/* Mobile Close Button */}
              {isMobile && (
                <button
                  onClick={() => setSidebarVisible(false)}
                  style={{
                    background: 'none',
                    border: `1px solid ${theme.colors.border}`,
                    borderRadius: '6px',
                    width: '32px',
                    height: '32px',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    cursor: 'pointer',
                    color: theme.colors.text
                  }}
                >
                  ✕
                </button>
              )}
            </div>
          </div>
          
          {/* Sidebar Content */}
          <div style={{ 
            padding: sidebarCollapsed ? '10px' : '20px',
            height: 'calc(100% - 80px)',
            overflowY: 'auto'
          }}>
            {sidebar}
          </div>
        </aside>
      )}

      {/* Main Content Area */}
      <main style={{
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        minWidth: 0, // Flex overflow fix
        marginLeft: isMobile && sidebarVisible ? '0' : '0'
      }}>
        
        {/* Mobile Filter Toggle */}
        {isMobile && !sidebarVisible && (
          <div style={{
            position: 'fixed',
            bottom: '20px',
            right: '20px',
            zIndex: 1000
          }}>
            <button
              onClick={() => setSidebarVisible(true)}
              style={{
                width: '56px',
                height: '56px',
                borderRadius: '50%',
                background: `linear-gradient(135deg, ${theme.colors.primary} 0%, #764ba2 100%)`,
                border: 'none',
                color: 'white',
                fontSize: '20px',
                cursor: 'pointer',
                boxShadow: '0 4px 20px rgba(0,0,0,0.2)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            >
              🎛️
            </button>
          </div>
        )}
        
        {/* Content Container */}
        <div style={{
          flex: 1,
          padding: '20px',
          maxWidth: '100%',
          overflow: 'hidden'
        }}>
          {children}
        </div>
        
        {/* Footer */}
        <footer style={{
          padding: '20px',
          textAlign: 'center',
          borderTop: `1px solid ${theme.colors.border}`,
          backgroundColor: theme.colors.surface,
          color: theme.colors.textSecondary,
          fontSize: '14px'
        }}>
          <div style={{ 
            display: 'flex', 
            justifyContent: 'center',
            alignItems: 'center',
            gap: '15px',
            flexWrap: 'wrap'
          }}>
            <span>© 2025 NakitAkış Analytics</span>
            <span style={{ opacity: 0.5 }}>•</span>
            <span>React + .NET + PostgreSQL</span>
            <span style={{ opacity: 0.5 }}>•</span>
            <span>v1.0.0</span>
          </div>
        </footer>
      </main>

      {/* Right Panel (Optional) */}
      {rightPanel && !isMobile && (
        <aside style={{
          width: '300px',
          backgroundColor: theme.colors.surface,
          borderLeft: `1px solid ${theme.colors.border}`,
          overflowY: 'auto'
        }}>
          <div style={{ padding: '20px' }}>
            {rightPanel}
          </div>
        </aside>
      )}
      
      {/* Mobile Overlay */}
      {isMobile && sidebarVisible && (
        <div 
          style={{
            position: 'fixed',
            top: '70px',
            left: '0',
            right: '0',
            bottom: '0',
            backgroundColor: 'rgba(0,0,0,0.5)',
            zIndex: 998
          }}
          onClick={() => setSidebarVisible(false)}
        />
      )}
    </div>
  );
};

export default LayoutContainer;