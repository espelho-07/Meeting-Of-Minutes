import { AlertCircle, ArrowRight, CalendarRange, UserCheck, Users } from "lucide-react";
import { Surface } from "../../components/ui/surface";
import { Badge } from "../../components/ui/badge";
import { Button } from "../../components/ui/button";
import { queueItems } from "../../lib/mock-data";
import { SectionHeader } from "../../lib/ui";

const stats = [
  { label: "Meetings live", value: "48", delta: "+12%", icon: CalendarRange },
  { label: "Open approvals", value: "06", delta: "24h SLA", icon: AlertCircle },
  { label: "Attendance coverage", value: "94%", delta: "Last 30 days", icon: UserCheck },
  { label: "Staff active", value: "182", delta: "Across 7 departments", icon: Users }
];

export function DashboardPage() {
  return (
    <div className="space-y-6">
      <SectionHeader
        eyebrow="Operations overview"
        title="A calmer command center for meeting operations"
        description="The dashboard is designed to surface urgency, not just metrics. Approvals, scheduling health, and operational risks all sit in one restrained, high-signal surface."
        actions={
          <>
            <Button variant="secondary">Export report</Button>
            <Button>Schedule meeting</Button>
          </>
        }
      />

      <section className="grid gap-4 lg:grid-cols-[1.4fr_0.9fr]">
        <Surface className="overflow-hidden bg-[radial-gradient(circle_at_top_left,rgba(99,102,241,0.18),transparent_36%),linear-gradient(180deg,rgba(255,255,255,0.92),rgba(255,255,255,0.76))] dark:bg-[radial-gradient(circle_at_top_left,rgba(99,102,241,0.18),transparent_34%),linear-gradient(180deg,rgba(17,24,39,0.96),rgba(15,23,42,0.88))]">
          <div className="space-y-4">
            <Badge>Live operational posture</Badge>
            <div className="max-w-2xl space-y-3">
              <h2 className="text-3xl font-semibold tracking-[-0.04em] text-ink sm:text-4xl">Your meeting system should feel quiet, fast, and deeply under control.</h2>
              <p className="max-w-xl text-sm leading-7 text-muted sm:text-base">
                This shell treats approvals, risk signals, attendance posture, and meeting throughput as one connected workflow instead of disconnected admin pages.
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Badge>6 approvals pending</Badge>
              <Badge tone="warning">2 items overdue 24h+</Badge>
              <Badge tone="danger">1 high-risk sign-in pattern</Badge>
            </div>
          </div>
        </Surface>

        <Surface>
          <div className="space-y-5">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">Action queue</div>
              <h3 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-ink">Clear the highest-impact work first</h3>
            </div>
            <div className="space-y-3">
              {queueItems.slice(0, 3).map((item) => (
                <div key={item.id} className="rounded-3xl border border-line bg-canvas/70 p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="text-sm font-semibold text-ink">{item.title}</div>
                      <p className="mt-1 text-sm leading-6 text-muted">{item.description}</p>
                    </div>
                    <Badge tone={item.priority === "Critical" ? "danger" : item.priority === "Stale" ? "warning" : "default"}>{item.priority}</Badge>
                  </div>
                </div>
              ))}
            </div>
            <Button variant="secondary" className="w-full justify-between">
              Open Action Center <ArrowRight className="h-4 w-4" />
            </Button>
          </div>
        </Surface>
      </section>

      <section className="grid gap-4 xl:grid-cols-4">
        {stats.map((stat) => {
          const Icon = stat.icon;
          return (
            <Surface key={stat.label} className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="grid h-11 w-11 place-items-center rounded-2xl bg-ink text-white shadow-lg shadow-brand/15">
                  <Icon className="h-4 w-4" />
                </div>
                <Badge>{stat.delta}</Badge>
              </div>
              <div>
                <div className="text-4xl font-semibold tracking-[-0.05em] text-ink">{stat.value}</div>
                <div className="mt-2 text-sm text-muted">{stat.label}</div>
              </div>
            </Surface>
          );
        })}
      </section>

      <section className="grid gap-4 xl:grid-cols-[1.1fr_0.9fr]">
        <Surface>
          <div className="mb-6 flex items-center justify-between">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">Workload by department</div>
              <h3 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-ink">Operational load</h3>
            </div>
            <Badge>Updated just now</Badge>
          </div>
          <div className="space-y-4">
            {[
              ["Product", 86],
              ["Operations", 62],
              ["Finance", 41],
              ["Academic Ops", 73]
            ].map(([label, value]) => (
              <div key={label} className="space-y-2">
                <div className="flex items-center justify-between text-sm text-muted">
                  <span>{label}</span>
                  <span>{value}%</span>
                </div>
                <div className="h-3 overflow-hidden rounded-full bg-canvas">
                  <div className="h-full rounded-full bg-gradient-to-r from-brand via-accent to-sky-400" style={{ width: `${value}%` }} />
                </div>
              </div>
            ))}
          </div>
        </Surface>

        <Surface>
          <div className="mb-6">
            <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">System notes</div>
            <h3 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-ink">What changed today</h3>
          </div>
          <div className="space-y-4">
            {[
              ["Profile request approved", "Neel Patel profile update was approved 12 minutes ago."],
              ["Transfer request received", "Operations to Finance transfer arrived with supporting document."],
              ["Attendance coverage improved", "Attendance logging is now above 90% for all visible meetings."]
            ].map(([title, text]) => (
              <div key={title} className="rounded-3xl border border-line bg-canvas/70 p-4">
                <div className="text-sm font-semibold text-ink">{title}</div>
                <p className="mt-2 text-sm leading-6 text-muted">{text}</p>
              </div>
            ))}
          </div>
        </Surface>
      </section>
    </div>
  );
}
