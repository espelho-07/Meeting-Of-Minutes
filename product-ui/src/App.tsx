import { useMemo, useState } from "react";
import { AppShell } from "./components/layout/app-shell";
import { Button } from "./components/ui/button";
import { DashboardPage } from "./pages/dashboard/dashboard-page";
import { MeetingsListPage } from "./pages/meetings/meetings-list-page";
import { MeetingTypePage } from "./pages/meeting-types/meeting-type-page";
import { ActionCenterPage } from "./pages/action-center/action-center-page";

const pages = [
  { id: "dashboard", label: "Dashboard" },
  { id: "all-meetings", label: "All Meetings" },
  { id: "meeting-types", label: "Meeting Types" },
  { id: "action-center", label: "Action Center" }
] as const;

type PageId = (typeof pages)[number]["id"];

export default function App() {
  const [page, setPage] = useState<PageId>("dashboard");

  const content = useMemo(() => {
    if (page === "all-meetings") return <MeetingsListPage />;
    if (page === "meeting-types") return <MeetingTypePage />;
    if (page === "action-center") return <ActionCenterPage />;
    return <DashboardPage />;
  }, [page]);

  const pageLabel = pages.find((item) => item.id === page)?.label ?? "Dashboard";

  return (
    <AppShell activePage={page} pageLabel={pageLabel}>
      <div className="flex flex-wrap gap-3">
        {pages.map((item) => (
          <Button key={item.id} variant={item.id === page ? "primary" : "secondary"} onClick={() => setPage(item.id)}>
            {item.label}
          </Button>
        ))}
      </div>
      {content}
    </AppShell>
  );
}
