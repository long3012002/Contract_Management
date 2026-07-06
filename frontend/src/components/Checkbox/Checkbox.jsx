import React from 'react';

/**
 * Reusable Checkbox component.
 */
const Checkbox = React.forwardRef(({
  label,
  id,
  disabled = false,
  className = '',
  ...props
}, ref) => {
  return (
    <label htmlFor={id} className={`flex items-center gap-2 text-muted-foreground cursor-pointer select-none ${className}`}>
      <input
        type="checkbox"
        id={id}
        ref={ref}
        disabled={disabled}
        className="w-4 h-4 rounded text-primary border-input focus:ring-primary/20 accent-primary disabled:opacity-50"
        {...props}
      />
      {label && <span>{label}</span>}
    </label>
  );
});

Checkbox.displayName = 'Checkbox';

export default Checkbox;
