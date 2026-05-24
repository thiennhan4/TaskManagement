import React from 'react';
import { Moon, Sun } from 'lucide-react';
import { useTheme } from '@/context/ThemeContext';

export default function ThemeToggle() {
  const { isDark, toggleTheme } = useTheme();

  return (
    <button
      type="button"
      role="switch"
      aria-checked={isDark}
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
      title={isDark ? 'Light mode' : 'Dark mode'}
      onClick={toggleTheme}
      className={`
        relative inline-flex h-10 w-[78px] shrink-0 cursor-pointer items-center rounded-full
        border border-border-subtle bg-white/80 p-1 text-text-main shadow-sm transition-all duration-300 ease-in-out
        hover:border-primary/40 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary/30
        dark:bg-slate-900/80
      `}
    >
      <span className={`absolute left-3 transition-opacity ${isDark ? 'opacity-40' : 'opacity-100'}`}>
        <Sun size={15} className="text-amber-500" />
      </span>
      <span className={`absolute right-3 transition-opacity ${isDark ? 'opacity-100' : 'opacity-40'}`}>
        <Moon size={15} className="text-slate-500 dark:text-slate-300" />
      </span>

      <div
        className={`
          pointer-events-none absolute left-1 flex h-8 w-8 transform items-center justify-center rounded-full
          bg-primary text-white shadow-md transition-transform duration-300 ease-in-out
          ${isDark ? 'translate-x-[38px]' : 'translate-x-0'}
        `}
      >
        {isDark ? <Moon size={16} /> : <Sun size={16} />}
      </div>
    </button>
  );
}
