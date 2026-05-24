import React from 'react';

const Select = React.forwardRef(({ label, error, children, className = '', ...props }, ref) => {
  return (
    <div className="flex flex-col gap-1.5 w-full">
      {label && (
        <label className="text-sm font-bold text-slate-700 dark:text-slate-300 ml-1">
          {label}
        </label>
      )}
      <select
        ref={ref}
        className={`
          px-4 py-2.5 rounded-xl border transition-all text-sm appearance-none bg-no-repeat bg-[right_1rem_center]
          ${error 
            ? 'border-red-500 focus:ring-red-200' 
            : 'border-slate-200 focus:border-primary focus:ring-4 focus:ring-primary/10'}
          bg-white dark:bg-slate-800 dark:border-slate-700
          ${className}
        `}
        style={{ backgroundImage: 'url("data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' fill=\'none\' viewBox=\'0 0 24 24\' stroke=\'%2364748b\' stroke-width=\'2\'%3E%3Cpath stroke-linecap=\'round\' stroke-linejoin=\'round\' d=\'M19 9l-7 7-7-7\' /%3E%3C/svg%3E")', backgroundSize: '1.25rem' }}
        {...props}
      >
        {children}
      </select>
      {error && <span className="text-xs font-bold text-red-500 ml-1">{error}</span>}
    </div>
  );
});

Select.displayName = 'Select';

export default Select;
