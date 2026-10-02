import React from 'react';

const Select = React.forwardRef(({ label, error, children, className = '', id: suppliedId, 'aria-describedby': describedBy, ...props }, ref) => {
  const generatedId = React.useId();
  const id = suppliedId || generatedId;
  const errorId = `${id}-error`;
  const description = [describedBy, error ? errorId : null].filter(Boolean).join(' ') || undefined;
  return (
    <div className="flex flex-col gap-1.5 w-full">
      {label && (
        <label htmlFor={id} className="text-sm font-bold text-text-main ml-1">
          {label}
        </label>
      )}
      <select
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={description}
        className={`
          px-4 py-2.5 rounded-xl border transition-all text-sm appearance-none bg-no-repeat bg-[right_1rem_center]
          text-text-main bg-surface-0 border-border-subtle
          focus:outline-none disabled:opacity-50 disabled:bg-surface-2
          ${error 
            ? 'border-red-500 focus:ring-red-200' 
            : 'focus:border-primary focus:ring-4 focus:ring-primary/10'}
          ${className}
        `}
        style={{ backgroundImage: 'url("data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' fill=\'none\' viewBox=\'0 0 24 24\' stroke=\'%2364748b\' stroke-width=\'2\'%3E%3Cpath stroke-linecap=\'round\' stroke-linejoin=\'round\' d=\'M19 9l-7 7-7-7\' /%3E%3C/svg%3E")', backgroundSize: '1.25rem' }}
        {...props}
      >
        {children}
      </select>
      {error && <span id={errorId} className="text-xs font-bold text-red-500 ml-1">{error}</span>}
    </div>
  );
});

Select.displayName = 'Select';

export default Select;
