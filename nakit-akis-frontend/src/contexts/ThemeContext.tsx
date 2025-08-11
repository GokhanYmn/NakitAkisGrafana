import React, { createContext, useContext, useState, useEffect } from 'react';

interface ThemeContextType {
  isDark: boolean;
  toggleTheme: () => void;
  theme: Theme;
}

interface Theme {
  name: string;
  colors: {
    primary: string;
    secondary: string;
    background: string;
    surface: string;
    text: string;
    textSecondary: string;
    border: string;
    success: string;
    warning: string;
    error: string;
    chart: {
      mevduat: string;
      faiz: string;
      background: string;
    };
  };
}

const lightTheme: Theme = {
  name: 'light',
  colors: {
    primary: '#007bff',
    secondary: '#6c757d',
    background: '#ffffff',
    surface: '#f8f9fa',
    text: '#212529',
    textSecondary: '#6c757d',
    border: '#dee2e6',
    success: '#28a745',
    warning: '#ffc107',
    error: '#dc3545',
    chart: {
      mevduat: 'rgb(75, 192, 192)',
      faiz: 'rgb(255, 99, 132)',
      background: '#ffffff'
    }
  }
};

const darkTheme: Theme = {
  name: 'dark',
  colors: {
    primary: '#0d6efd',
    secondary: '#6c757d',
    background: '#121212',
    surface: '#1e1e1e',
    text: '#ffffff',
    textSecondary: '#adb5bd',
    border: '#343a40',
    success: '#198754',
    warning: '#fd7e14',
    error: '#dc3545',
    chart: {
      mevduat: 'rgb(100, 220, 220)',
      faiz: 'rgb(255, 120, 150)',
      background: '#1e1e1e'
    }
  }
};

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isDark, setIsDark] = useState<boolean>(() => {
    // Local storage'dan tema tercihini oku
    const saved = localStorage.getItem('theme');
    return saved ? saved === 'dark' : false;
  });

  const theme = isDark ? darkTheme : lightTheme;

  const toggleTheme = () => {
    setIsDark(prev => {
      const newTheme = !prev;
      localStorage.setItem('theme', newTheme ? 'dark' : 'light');
      return newTheme;
    });
  };

  // Body'ye tema class'ı ekle
  useEffect(() => {
    document.body.className = isDark ? 'dark-theme' : 'light-theme';
    document.body.style.backgroundColor = theme.colors.background;
    document.body.style.color = theme.colors.text;
  }, [isDark, theme]);

  return (
    <ThemeContext.Provider value={{ isDark, toggleTheme, theme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};