import React from 'react';
import { Languages } from 'lucide-react';
import { useLanguage } from '@/context/LanguageContext';

export default function LanguageSwitcher({ tone = 'light', compact = false, className = '' }) {
  const { language, toggleLanguage, t } = useLanguage();
  const isDark = tone === 'dark';
  const nextLabel = language === 'en' ? t('common.vietnamese') : t('common.english');

  return (
    <button
      type="button"
      onClick={toggleLanguage}
      aria-label={`${t('common.language')}: ${nextLabel}`}
      title={nextLabel}
      className={`inline-flex h-9 items-center justify-center gap-2 rounded-full border px-3 text-xs font-black uppercase tracking-[0.12em] transition-all ${
        isDark
          ? 'border-white/10 bg-white/5 text-white hover:border-cyan-300/50 hover:bg-white/10'
          : 'border-slate-200 bg-white/70 text-slate-700 shadow-sm hover:border-primary/40 hover:text-primary'
      } ${className}`}
    >
      <Languages size={15} />
      <span>{language === 'en' ? 'EN' : 'VI'}</span>
      {!compact && <span className={isDark ? 'text-white/40' : 'text-slate-300'}>/</span>}
      {!compact && <span className={isDark ? 'text-white/45' : 'text-slate-400'}>{language === 'en' ? 'VI' : 'EN'}</span>}
    </button>
  );
}
