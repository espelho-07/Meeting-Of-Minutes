import { Badge } from "../../components/ui/badge";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Surface } from "../../components/ui/surface";
import { meetingRows } from "../../lib/mock-data";
import { SectionHeader } from "../../lib/ui";

export function MeetingsListPage() {
  return (
    <div className="space-y-6">
      <SectionHeader
        eyebrow="Meeting index"
        title="Every meeting in one high-signal table"
        description="Filters, search, status density, and quick actions should make this screen feel closer to Linear than a typical CRUD list."
        actions={
          <>
            <Button variant="secondary">Export</Button>
            <Button>New meeting</Button>
          </>
        }
      />

      <Surface className="space-y-6">
        <div className="grid gap-4 lg:grid-cols-[1.2fr_auto_auto_auto]">
          <Input placeholder="Search meetings, venue, or department" />
          <Button variant="secondary">All statuses</Button>
          <Button variant="secondary">All departments</Button>
          <Button variant="secondary">Sort: Newest</Button>
        </div>

        <div className="overflow-hidden rounded-[24px] border border-line">
          <table className="min-w-full divide-y divide-line text-left">
            <thead className="bg-canvas/80 text-xs font-semibold uppercase tracking-[0.18em] text-muted">
              <tr>
                <th className="px-5 py-4">Meeting</th>
                <th className="px-5 py-4">Department</th>
                <th className="px-5 py-4">Type</th>
                <th className="px-5 py-4">Venue</th>
                <th className="px-5 py-4">Status</th>
                <th className="px-5 py-4">Time</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-line bg-panel text-sm">
              {meetingRows.map((row) => (
                <tr key={row.id} className="transition hover:bg-canvas/70">
                  <td className="px-5 py-4">
                    <div className="font-medium text-ink">{row.title}</div>
                    <div className="mt-1 text-xs text-muted">{row.id}</div>
                  </td>
                  <td className="px-5 py-4 text-muted">{row.department}</td>
                  <td className="px-5 py-4 text-muted">{row.type}</td>
                  <td className="px-5 py-4 text-muted">{row.venue}</td>
                  <td className="px-5 py-4">
                    <Badge tone={row.status === "Cancelled" ? "danger" : row.status === "Completed" ? "success" : "default"}>{row.status}</Badge>
                  </td>
                  <td className="px-5 py-4 text-muted">{row.time}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Surface>
    </div>
  );
}
