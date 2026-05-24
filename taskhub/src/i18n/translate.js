import { translations } from './translations';

const STORAGE_KEY = 'taskhub-language';

export function getCurrentLanguage() {
  if (typeof window === 'undefined') {
    return 'en';
  }

  const saved = window.localStorage.getItem(STORAGE_KEY);
  return saved && translations[saved] ? saved : 'en';
}

export function translate(key, params = {}, language = getCurrentLanguage()) {
  const template = translations[language]?.[key] || translations.en[key] || key;

  return Object.entries(params).reduce(
    (text, [param, value]) => text.replaceAll(`{${param}}`, value),
    template,
  );
}
