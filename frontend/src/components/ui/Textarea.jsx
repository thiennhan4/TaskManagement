import React from 'react';

const Textarea = React.forwardRef(({ label, error, className = '', ...props }, ref) => {
  return (
    <div className="flex flex-col gap-1.5 w-full">
      {label && (
        <label className="text-sm font-bold text-text-main ml-1">
          {label}
        </label>
      )}
      <textarea
        ref={ref}
        className={`
          px-4 py-2.5 rounded-xl border transition-all min-h-[100px] text-sm
          text-text-main placeholder:text-text-muted bg-surface-0 border-border-subtle
          focus:outline-none disabled:opacity-50 disabled:bg-surface-2
          ${error 
            ? 'border-red-500 focus:ring-red-200' 
            : 'focus:border-primary focus:ring-4 focus:ring-primary/10'}
          ${className}
        `}
        {...props}
      />
      {error && <span className="text-xs font-bold text-red-500 ml-1">{error}</span>}
    </div>
  );
});

Textarea.displayName = 'Textarea';

export default Textarea;
