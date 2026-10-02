import { useEffect, useRef } from 'react';

export default function Dropdown({ label, children }) {
  const root = useRef(null);
  useEffect(() => {
    const outside = event => { if (!root.current?.contains(event.target)) root.current?.removeAttribute('open'); };
    const escape = event => { if (event.key === 'Escape' && root.current?.open) { root.current.removeAttribute('open'); root.current.querySelector('summary').focus(); } };
    document.addEventListener('pointerdown', outside);
    document.addEventListener('keydown', escape);
    return () => { document.removeEventListener('pointerdown', outside); document.removeEventListener('keydown', escape); };
  }, []);
  return <details ref={root} className="relative" onClick={event => { if (event.target.closest('a,button')) root.current.removeAttribute('open'); }}>
    <summary className="cursor-pointer list-none rounded-xl focus-visible:outline-2 focus-visible:outline-primary">{label}</summary>
    <div className="absolute top-full right-0 mt-2 w-48 bg-surface-0 rounded-2xl shadow-premium border border-border-subtle p-2 z-50">{children}</div>
  </details>;
}
