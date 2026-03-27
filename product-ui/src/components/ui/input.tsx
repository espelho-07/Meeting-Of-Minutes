import { InputHTMLAttributes } from "react";
import { cn } from "../../lib/ui";

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label?: string;
  hint?: string;
};

export function Input({ label, hint, className, ...props }: InputProps) {
  return (
    <label className="block space-y-2">
      {label ? <span className="text-sm font-medium text-ink">{label}</span> : null}
      <input
        className={cn(
          "h-12 w-full rounded-2xl border border-line bg-panel px-4 text-sm text-ink shadow-sm shadow-white/40 outline-none transition duration-200 placeholder:text-muted focus:border-brand/40 focus:ring-4 focus:ring-brand/10 dark:shadow-none",
          className
        )}
        {...props}
      />
      {hint ? <span className="text-xs text-muted">{hint}</span> : null}
    </label>
  );
}
