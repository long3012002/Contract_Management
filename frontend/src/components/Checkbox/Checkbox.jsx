import * as React from "react"
import { cn } from "../../lib/utils"

const Checkbox = React.forwardRef(({
  label,
  id,
  disabled = false,
  className = "",
  ...props
}, ref) => {
  const checkboxId = id || props.name;

  return (
    <div className={cn("flex items-center gap-2 select-none", className)}>
      <input
        type="checkbox"
        id={checkboxId}
        ref={ref}
        disabled={disabled}
        className={cn(
          "h-4 w-4 shrink-0 rounded-sm border border-input bg-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 accent-primary cursor-pointer transition-colors"
        )}
        {...props}
      />
      {label && (
        <label
          htmlFor={checkboxId}
          className="text-sm font-medium text-muted-foreground leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
        >
          {label}
        </label>
      )}
    </div>
  )
})
Checkbox.displayName = "Checkbox"

export { Checkbox }
export default Checkbox
