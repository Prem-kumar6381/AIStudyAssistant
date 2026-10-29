import React, { createContext, useState, useEffect, useCallback } from 'react';

export const ThemeContext = createContext(null);

const THEME_STORAGE_KEY = 'nexora_theme';

export function ThemeProvider({ children }) {
  const [theme, setThemeState] = useState(() => {
    try {
      const saved = localStorage.getItem(THEME_STORAGE_KEY) || localStorage.getItem('aivora_theme');
      if (saved === 'dark' || saved === 'light') {
        return saved;
      }
    } catch {
      // Ignore storage errors
    }
    return 'light'; // Default to light theme
  });

  const applyTheme = useCallback((targetTheme) => {
    document.documentElement.setAttribute('data-theme', targetTheme);
    try {
      localStorage.setItem(THEME_STORAGE_KEY, targetTheme);
    } catch {
      // Ignore storage errors
    }
  }, []);

  useEffect(() => {
    applyTheme(theme);
  }, [theme, applyTheme]);

  const toggleTheme = () => {
    setThemeState((prev) => (prev === 'light' ? 'dark' : 'light'));
  };

  const setTheme = (newTheme) => {
    if (newTheme === 'light' || newTheme === 'dark') {
      setThemeState(newTheme);
    }
  };

  const value = {
    theme,
    isDark: theme === 'dark',
    toggleTheme,
    setTheme,
  };

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export default ThemeContext;
