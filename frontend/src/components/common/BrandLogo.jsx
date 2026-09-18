import React from 'react';
import { Link } from 'react-router-dom';

const sizeStyles = {
  sm: {
    mark: 'h-8 w-8 rounded-xl',
    text: 'text-lg',
    gap: 'gap-2',
  },
  md: {
    mark: 'h-10 w-10 rounded-2xl',
    text: 'text-2xl',
    gap: 'gap-2.5',
  },
  lg: {
    mark: 'h-12 w-12 rounded-[1.15rem]',
    text: 'text-3xl',
    gap: 'gap-3',
  },
};

export default function BrandLogo({
  size = 'md',
  tone = 'light',
  showText = true,
  className = '',
  to = '/',
}) {
  const styles = sizeStyles[size] || sizeStyles.md;
  const isDark = tone === 'dark';

  return (
    <Link to={to} className={`inline-flex items-center ${styles.gap} ${className}`} aria-label="TaskHub">
      <span
        className={`${styles.mark} relative grid shrink-0 place-items-center overflow-hidden border shadow-[0_14px_35px_rgba(79,70,229,0.28)] ${
          isDark ? 'border-white/15' : 'border-indigo-200/70'
        }`}
      >
        <span className="absolute inset-0 bg-[linear-gradient(135deg,#4F46E5_0%,#6366F1_46%,#06B6D4_100%)]" />
        <span className="absolute -right-4 -top-4 h-9 w-9 rounded-full bg-cyan-200/40 blur-xl" />
        <svg className="relative h-[58%] w-[58%]" viewBox="0 0 28 28" fill="none" aria-hidden="true">
          <rect x="5" y="6" width="8" height="7" rx="2" stroke="white" strokeWidth="2.2" />
          <rect x="15" y="15" width="8" height="7" rx="2" stroke="white" strokeWidth="2.2" />
          <path d="M13 9.5h3.2c2.6 0 4.7 2.1 4.7 4.7V15" stroke="white" strokeWidth="2.2" strokeLinecap="round" />
          <path d="M15 18.5h-3.2A4.7 4.7 0 0 1 7.1 13.8V13" stroke="white" strokeWidth="2.2" strokeLinecap="round" />
          <circle cx="21" cy="9" r="2" fill="#CFFAFE" />
          <circle cx="7" cy="19" r="2" fill="#CFFAFE" />
        </svg>
      </span>

      {showText && (
        <span className={`flex items-baseline font-black ${styles.text} leading-none tracking-[-0.045em] ${isDark ? 'text-white' : 'text-slate-950'}`}>
          <span>Task</span>
          <span className="bg-gradient-to-r from-indigo-600 to-cyan-500 bg-clip-text text-transparent">Hub</span>
        </span>
      )}
    </Link>
  );
}
