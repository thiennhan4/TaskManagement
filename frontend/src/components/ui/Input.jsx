import React from 'react';
import PropTypes from 'prop-types';

const Input = React.forwardRef(({ label, error, className = '', id: suppliedId, 'aria-describedby': describedBy, ...props }, ref) => {
  const generatedId = React.useId();
  const id = suppliedId || generatedId;
  const errorId = `${id}-error`;
  const description = [describedBy, error ? errorId : null].filter(Boolean).join(' ') || undefined;
  return (
    <div className={`flex flex-col gap-1.5 w-full ${className}`}>
      {label && (
        <label htmlFor={id} className="text-sm font-bold text-text-main ml-1">
          {label}
        </label>
      )}
      <input
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={description}
        className={`
          px-4 py-2.5 rounded-xl border border-border-subtle bg-surface-0
          text-text-main placeholder:text-text-muted
          focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary
          transition-all duration-200
          ${error ? 'border-rose-500 focus:ring-rose-500/20 focus:border-rose-500' : ''}
          disabled:opacity-50 disabled:bg-surface-2
        `}
        {...props}
      />
      {error && (
        <span id={errorId} className="text-xs font-medium text-rose-500 ml-1">
          {error}
        </span>
      )}
    </div>
  );
});

Input.displayName = 'Input';

Input.propTypes = {
  label: PropTypes.string,
  error: PropTypes.string,
  className: PropTypes.string,
};

export default Input;
