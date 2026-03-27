import { ReactNode, useMemo, useState } from "react";
import { Sidebar } from "./sidebar";
import { Topbar } from "./topbar";
import { queueItems } from "../../lib/mock-data";

export function AppShell({
  activePage,
  pageLabel,
  children
}: {
  activePage: string;
  pageLabel: string;
  children: ReactNode;
}) {
  const [darkMode, setDarkMode] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const queueCount = useMemo(() => queueItems.length, []);

  return (
    <div className={darkMode ? "dark" : ""}>
      <div className="mx-auto min-h-screen max-w-[1680px] px-4 py-6 sm:px-6 lg:px-8">
        <div className="flex gap-6">
          <Sidebar activePage={activePage} collapsed={collapsed} />
          <div className="min-w-0 flex-1 space-y-6">
            <Topbar
              pageLabel={pageLabel}
              queueCount={queueCount}
              darkMode={darkMode}
              onToggleTheme={() => setDarkMode((current) => !current)}
              onToggleSidebar={() => setCollapsed((current) => !current)}
            />
            <main className="space-y-6">{children}</main>
          </div>
        </div>
      </div>
    </div>
  );
}
