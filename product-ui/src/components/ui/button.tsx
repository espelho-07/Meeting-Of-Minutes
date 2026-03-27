import { ButtonHTMLAttributes } from "react";
import { cn } from "../../lib/ui";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost" | "danger";
};

export function Button({ variant = "primary", className, ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        "inline-flex h-11 items-center justify-center rounded-2xl px-4 text-sm font-semibold transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand/40",
        variant === "primary" && "bg-ink text-white shadow-lg shadow-brand/20 hover:-translate-y-0.5 hover:bg-brand",
        variant === "secondary" && "border border-line bg-panel text-ink hover:border-brand/30 hover:bg-brand/5",
        variant === "ghost" && "text-muted hover:bg-panel hover:text-ink",
        variant === "danger" && "bg-danger text-white shadow-lg shadow-danger/20 hover:-translate-y-0.5 hover:bg-danger/90",
        className
      )}
      {...props}
    />
  );
}
