import { Bell, Command, MoonStar, Search, SunMedium } from "lucide-react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Badge } from "../ui/badge";

type TopbarProps = {
  pageLabel: string;
  queueCount: number;
  darkMode: boolean;
  onToggleTheme: () => void;
  onToggleSidebar: () => void;
};

export function Topbar({ pageLabel, queueCount, darkMode, onToggleTheme, onToggleSidebar }: TopbarProps) {
  return (
    <header className="sticky top-6 z-20 flex flex-wrap items-center gap-4 rounded-shell border border-line/80 bg-panel/85 px-5 py-4 shadow-panel backdrop-blur">
      <div className="flex items-center gap-3">
        <Button variant="secondary" className="xl:hidden" onClick={onToggleSidebar}>
          <Command className="mr-2 h-4 w-4" /> Menu
        </Button>
        <div>
          <div className="text-xs font-semibold uppercase tracking-[0.2em] text-muted">Command Surface</div>
          <div className="text-xl font-semibold tracking-[-0.03em] text-ink">{pageLabel}</div>
        </div>
      </div>

      <div className="min-w-[260px] flex-1">
        <Input placeholder="Search meetings, staff, requests" className="bg-canvas" />
      </div>

      <div className="ml-auto flex items-center gap-3">
        <div className="hidden items-center gap-2 rounded-2xl border border-line bg-canvas/80 px-3 py-2 lg:flex">
          <Search className="h-4 w-4 text-muted" />
          <span className="text-sm text-muted">Press</span>
          <Badge>Ctrl K</Badge>
        </div>
        <button className="relative grid h-11 w-11 place-items-center rounded-2xl border border-line bg-canvas text-muted transition hover:text-ink">
          <Bell className="h-4 w-4" />
          {queueCount > 0 ? <span className="absolute right-1 top-1 h-2.5 w-2.5 rounded-full bg-brand" /> : null}
        </button>
        <button className="grid h-11 w-11 place-items-center rounded-2xl border border-line bg-canvas text-muted transition hover:text-ink" onClick={onToggleTheme}>
          {darkMode ? <SunMedium className="h-4 w-4" /> : <MoonStar className="h-4 w-4" />}
        </button>
        <div className="flex items-center gap-3 rounded-2xl border border-line bg-canvas/80 px-3 py-2">
          <div className="grid h-9 w-9 place-items-center rounded-2xl bg-ink text-sm font-semibold text-white">NP</div>
          <div className="hidden sm:block">
            <div className="text-sm font-medium text-ink">Neel Patel</div>
            <div className="text-xs text-muted">Founder Workspace</div>
          </div>
        </div>
      </div>
    </header>
  );
}
