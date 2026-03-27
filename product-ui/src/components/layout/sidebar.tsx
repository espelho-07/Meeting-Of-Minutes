import {
  Activity,
  Building2,
  CalendarPlus2,
  CalendarRange,
  History,
  LayoutDashboard,
  MapPin,
  Sparkles,
  Tags,
  UserCheck,
  Users
} from "lucide-react";
import { navSections } from "../../lib/mock-data";
import { cn } from "../../lib/ui";

const icons = {
  LayoutDashboard,
  Sparkles,
  History,
  Tags,
  Building2,
  MapPin,
  Users,
  CalendarRange,
  CalendarPlus2,
  UserCheck,
  Activity
};

type SidebarProps = {
  activePage: string;
  collapsed: boolean;
};

export function Sidebar({ activePage, collapsed }: SidebarProps) {
  return (
    <aside
      className={cn(
        "sticky top-6 hidden h-[calc(100vh-3rem)] flex-col rounded-shell border border-line/70 bg-panel/90 p-4 shadow-panel backdrop-blur xl:flex",
        collapsed ? "w-24" : "w-72"
      )}
    >
      <div className="mb-8 flex items-center gap-3 rounded-3xl border border-line bg-canvas/70 p-3">
        <div className="grid h-11 w-11 place-items-center rounded-2xl bg-ink text-sm font-semibold text-white">M</div>
        {!collapsed ? (
          <div>
            <div className="text-sm font-semibold text-ink">Meeting Of Minutes</div>
            <div className="text-xs text-muted">Operational workspace</div>
          </div>
        ) : null}
      </div>

      <nav className="flex-1 space-y-6 overflow-y-auto pr-1">
        {navSections.map((section) => (
          <div key={section.label} className="space-y-2">
            {!collapsed ? <div className="px-3 text-[11px] font-semibold uppercase tracking-[0.18em] text-muted">{section.label}</div> : null}
            <div className="space-y-1">
              {section.items.map((item) => {
                const Icon = icons[item.icon as keyof typeof icons];
                const isActive = activePage === item.id;
                return (
                  <button
                    key={item.id}
                    className={cn(
                      "group flex w-full items-center gap-3 rounded-2xl px-3 py-3 text-left text-sm font-medium transition-all duration-200",
                      isActive
                        ? "bg-ink text-white shadow-lg shadow-brand/15"
                        : "text-muted hover:bg-canvas hover:text-ink"
                    )}
                    type="button"
                  >
                    <Icon className={cn("h-4 w-4 shrink-0", isActive ? "text-white" : "text-muted transition group-hover:text-ink")} />
                    {!collapsed ? <span className="flex-1">{item.label}</span> : null}
                    {!collapsed && item.count ? (
                      <span className={cn("rounded-full px-2 py-0.5 text-[10px] font-semibold", isActive ? "bg-white/10 text-white" : "bg-brand/10 text-brand")}>{item.count}</span>
                    ) : null}
                  </button>
                );
              })}
            </div>
          </div>
        ))}
      </nav>
    </aside>
  );
}
