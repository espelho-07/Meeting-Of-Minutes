import { Badge } from "../../components/ui/badge";
import { Button } from "../../components/ui/button";
import { Surface } from "../../components/ui/surface";
import { queueItems } from "../../lib/mock-data";
import { SectionHeader } from "../../lib/ui";

export function ActionCenterPage() {
  return (
    <div className="space-y-6">
      <SectionHeader
        eyebrow="Execution layer"
        title="Action Center is where the operator actually gets work done"
        description="This screen turns the dashboard signals into a clean queue: prioritize, approve, reject, and keep context without navigating through scattered modules."
        actions={<Button>Open full activity</Button>}
      />

      <div className="grid gap-4 xl:grid-cols-[0.95fr_1.05fr]">
        <Surface className="space-y-5">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">Priority lane</div>
              <h3 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-ink">Handle these first</h3>
            </div>
            <Badge tone="warning">2 stale</Badge>
          </div>
          <div className="space-y-3">
            {queueItems.map((item) => (
              <div key={item.id} className="rounded-3xl border border-line bg-canvas/70 p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="text-sm font-semibold text-ink">{item.title}</div>
                    <p className="mt-1 text-sm leading-6 text-muted">{item.description}</p>
                    <div className="mt-3 text-xs uppercase tracking-[0.18em] text-muted">{item.meta}</div>
                  </div>
                  <Badge tone={item.priority === "Critical" ? "danger" : item.priority === "Stale" ? "warning" : "default"}>{item.priority}</Badge>
                </div>
              </div>
            ))}
          </div>
        </Surface>

        <Surface className="space-y-5">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">Quick review</div>
              <h3 className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-ink">Batch clear approvals without losing context</h3>
            </div>
            <Badge>6 selected</Badge>
          </div>
          <div className="rounded-3xl border border-dashed border-line bg-canvas/70 p-4 text-sm leading-7 text-muted">
            Bulk approval, shared remarks, and inline decision patterns make this screen feel closer to an operations console than a classic admin inbox.
          </div>
          <div className="space-y-4">
            {["Profile updates", "Transfer requests"].map((group) => (
              <div key={group} className="rounded-3xl border border-line bg-panel p-4">
                <div className="mb-3 flex items-center justify-between">
                  <div>
                    <div className="text-sm font-semibold text-ink">{group}</div>
                    <div className="text-xs text-muted">Shared note + multi-select action</div>
                  </div>
                  <Badge>{group === "Profile updates" ? "3 selected" : "2 selected"}</Badge>
                </div>
                <div className="flex flex-wrap gap-3">
                  <Button>Approve selected</Button>
                  <Button variant="secondary">Reject selected</Button>
                </div>
              </div>
            ))}
          </div>
        </Surface>
      </div>
    </div>
  );
}
