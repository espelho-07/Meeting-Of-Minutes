import { ReactNode } from "react";

export function cn(...classes: Array<string | false | null | undefined>) {
  return classes.filter(Boolean).join(" ");
}

export function panelClassName(extra?: string) {
  return cn(
    "rounded-shell border border-line/80 bg-panel/95 shadow-panel backdrop-blur-xl",
    extra
  );
}

export function SectionHeader({
  eyebrow,
  title,
  description,
  actions
}: {
  eyebrow: string;
  title: string;
  description: string;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
      <div className="max-w-2xl space-y-3">
        <div className="inline-flex items-center rounded-full border border-line bg-panel px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-muted">
          {eyebrow}
        </div>
        <div className="space-y-2">
          <h1 className="text-4xl font-semibold tracking-[-0.04em] text-ink sm:text-5xl">{title}</h1>
          <p className="max-w-xl text-sm leading-7 text-muted sm:text-base">{description}</p>
        </div>
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-3">{actions}</div> : null}
    </div>
  );
}
