import { createContext, useContext, useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';

const stack = [];
const Depth = createContext(0);
let background = [];
let previousOverflow;
const selector = 'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

export default function Modal({ isOpen, onClose, title, description, children, maxWidth = 'max-w-2xl', closeDisabled = false }) {
  const depth = useContext(Depth);
  const dialog = useRef(null);
  const close = useRef(onClose);
  const locked = useRef(closeDisabled);
  const titleId = useId();
  const descriptionId = useId();
  useEffect(() => { close.current = onClose; locked.current = closeDisabled; }, [onClose, closeDisabled]);
  useEffect(() => {
    if (!isOpen) return;
    const node = dialog.current;
    const previousFocus = document.activeElement;
    if (!stack.length) {
      previousOverflow = document.body.style.overflow; document.body.style.overflow = 'hidden';
      background = [...document.body.children].filter(element => !element.hasAttribute('data-modal-overlay')).map(element => [element, element.inert]);
      background.forEach(([element]) => { element.inert = true; });
    }
    const position = stack.findIndex(element => Number(element.dataset.modalDepth) > depth);
    if (position < 0) stack.push(node); else stack.splice(position, 0, node);
    const focusable = () => [...node.querySelectorAll(selector)].filter(el => !el.hidden && el.getAttribute('aria-hidden') !== 'true');
    if (stack.at(-1) === node) (node.querySelector('[autofocus]') || focusable()[0] || node).focus();
    const keydown = event => {
      if (stack.at(-1) !== node) return;
      if (event.key === 'Escape' && !locked.current) { event.preventDefault(); event.stopPropagation(); close.current?.(); }
      if (event.key === 'Tab') {
        const elements = focusable();
        const first = elements[0] || node, last = elements.at(-1) || node;
        if (event.shiftKey && (document.activeElement === first || document.activeElement === node)) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && (document.activeElement === last || !node.contains(document.activeElement))) { event.preventDefault(); first.focus(); }
      }
    };
    const focusin = event => {
      if (stack.at(-1) === node && !node.contains(event.target)) (focusable()[0] || node).focus();
    };
    document.addEventListener('keydown', keydown);
    document.addEventListener('focusin', focusin);
    return () => {
      document.removeEventListener('keydown', keydown);
      document.removeEventListener('focusin', focusin);
      const index = stack.indexOf(node);
      if (index >= 0) stack.splice(index, 1);
      if (!stack.length) { document.body.style.overflow = previousOverflow; background.forEach(([element, inert]) => { element.inert = inert; }); background = []; }
      if (previousFocus?.isConnected && (!stack.length || stack.at(-1).contains(previousFocus))) previousFocus.focus();
      else if (stack.length) (stack.at(-1).querySelector(selector) || stack.at(-1)).focus();
    };
  }, [isOpen, depth]);
  if (!isOpen) return null;
  return createPortal(<div data-modal-overlay className="fixed inset-0 z-[200] flex items-center justify-center p-4 bg-secondary/60 backdrop-blur-sm" onClick={event => { if (event.target === event.currentTarget && stack.at(-1) === dialog.current && !closeDisabled) onClose?.(); }}>
    <div ref={dialog} data-modal-depth={depth} role="dialog" aria-modal="true" aria-labelledby={titleId} aria-describedby={description ? descriptionId : undefined} tabIndex={-1} className={`w-full ${maxWidth} max-h-[92vh] bg-surface-0 rounded-3xl shadow-2xl flex flex-col border border-border-subtle`}>
      <div className="flex justify-between items-center px-6 py-5 border-b border-border-subtle shrink-0">
        <h2 id={titleId} className="text-xl font-bold text-text-main">{title}</h2>
        <button type="button" aria-label="Close dialog" disabled={closeDisabled} onClick={onClose} className="p-2 rounded-xl text-text-muted hover:bg-hover-bg disabled:opacity-50"><X size={20} /></button>
      </div>
      {description && <p id={descriptionId} className="px-6 pt-4 text-text-muted">{description}</p>}
      <div className="flex-1 overflow-y-auto"><Depth.Provider value={depth + 1}>{children}</Depth.Provider></div>
    </div>
  </div>, document.body);
}
