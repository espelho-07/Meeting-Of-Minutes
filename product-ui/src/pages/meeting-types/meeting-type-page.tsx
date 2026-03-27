import { Badge } from "../../components/ui/badge";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Surface } from "../../components/ui/surface";
import { SectionHeader } from "../../lib/ui";

export function MeetingTypePage() {
  return (
    <div className="space-y-6">
      <SectionHeader
        eyebrow="Configuration"
        title="Meeting type setup should feel editorial, not bureaucratic"
        description="The form keeps the structure crisp and the copy restrained so configuration work still feels premium and calm."
        actions={<Button variant="secondary">Preview taxonomy</Button>}
      />

      <div className="grid gap-4 lg:grid-cols-[1.2fr_0.8fr]">
        <Surface className="space-y-6">
          <div className="space-y-2">
            <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">Category details</div>
            <h2 className="text-2xl font-semibold tracking-[-0.03em] text-ink">Add a meeting type</h2>
          </div>
          <div className="grid gap-5">
            <Input label="Meeting type name" placeholder="For example: Faculty review" hint="Use names that still scan clearly inside lists, exports, and dashboard analytics." />
            <label className="block space-y-2">
              <span className="text-sm font-medium text-ink">Remarks</span>
              <textarea className="min-h-[140px] w-full rounded-3xl border border-line bg-panel px-4 py-3 text-sm text-ink outline-none transition duration-200 placeholder:text-muted focus:border-brand/40 focus:ring-4 focus:ring-brand/10" placeholder="Add usage notes, context, or reporting guidance." />
            </label>
            <div className="flex flex-wrap gap-3">
              <Button>Save Type</Button>
              <Button variant="secondary">Back to List</Button>
            </div>
          </div>
        </Surface>

        <Surface className="space-y-4">
          <div className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">Usage guidance</div>
          <h3 className="text-2xl font-semibold tracking-[-0.03em] text-ink">Design the taxonomy like a system, not a dropdown.</h3>
          <div className="space-y-3 text-sm leading-7 text-muted">
            <p>Shorter labels improve analytics, table scanning, and export readability.</p>
            <p>Use a clear difference between internal planning, review, compliance, and external meeting types.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Badge>Internal</Badge>
            <Badge>Review</Badge>
            <Badge>Client</Badge>
          </div>
        </Surface>
      </div>
    </div>
  );
}
