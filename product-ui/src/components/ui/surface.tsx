import { ReactNode } from "react";
import { cn, panelClassName } from "../../lib/ui";

export function Surface({ children, className }: { children: ReactNode; className?: string }) {
  return <section className={panelClassName(cn("p-6", className))}>{children}</section>;
}
