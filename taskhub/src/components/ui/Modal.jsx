import { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';

export default function Modal({ isOpen, onClose, title, children, maxWidth = 'max-w-2xl' }) {
  const overlayRef = useRef(null);

  useEffect(() => {
    if (!isOpen) return;
    const handleKey = (e) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', handleKey);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', handleKey);
      document.body.style.overflow = '';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return createPortal(
    <div
      ref={overlayRef}
      className="fixed inset-0 z-[200] flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={(e) => { if (e.target === overlayRef.current) onClose(); }}
    >
      <div className={`
        w-full ${maxWidth} max-h-[92vh] bg-white dark:bg-slate-900 rounded-3xl shadow-2xl flex flex-col 
        border border-white/20 dark:border-slate-800 animate-in zoom-in-95 slide-in-from-bottom-4 duration-300
      `}>
        {/* Header */}
        <div className="flex justify-between items-center px-8 py-5 border-b border-border-subtle dark:border-slate-800 shrink-0">
          <h2 className="text-xl font-bold text-text-main dark:text-white tracking-tight">{title}</h2>
          <button
            onClick={onClose}
            className="p-2 rounded-xl text-text-muted hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          >
            <X size={20} />
          </button>
        </div>
        
        {/* Scrollable body */}
        <div className="flex-1 overflow-y-auto">
          {children}
        </div>
      </div>
    </div>,
    document.body
  );
}
