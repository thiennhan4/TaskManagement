/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { translations } from '@/i18n/translations';

const STORAGE_KEY = 'taskhub-language';
const LanguageContext = createContext(null);

export function LanguageProvider({ children }) {
  const [language, setLanguageState] = useState(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    return saved && translations[saved] ? saved : 'en';
  });

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, language);
    document.documentElement.lang = language;
  }, [language]);

  const value = useMemo(() => {
    const setLanguage = (nextLanguage) => {
      if (translations[nextLanguage]) {
        setLanguageState(nextLanguage);
      }
    };

    const t = (key, params = {}) => {
      const template = translations[language]?.[key] || translations.en[key] || key;
      return Object.entries(params).reduce(
        (text, [param, value]) => text.replaceAll(`{${param}}`, value),
        template,
      );
    };

    return {
      language,
      setLanguage,
      toggleLanguage: () => setLanguage(language === 'en' ? 'vi' : 'en'),
      t,
    };
  }, [language]);

  return (
    <LanguageContext.Provider value={value}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  const context = useContext(LanguageContext);

  if (!context) {
    throw new Error('useLanguage must be used inside LanguageProvider');
  }

  return context;
}
