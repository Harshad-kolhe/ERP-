'use client';

import { useServerTable } from '@/components/data-table/use-server-table';

import { replaceOwnedTerms, valuesFor } from './filter-terms';
import { STATUS_TEXT } from './master-columns';
import { useStatusCounts } from './use-status-counts';

/**
 * The status band above a masters grid: how many records are in each state, and one
 * click to see them.
 *
 * This is where the counts live for any master that has a lifecycle. They are not
 * decoration — each one is the filter for that state, which is why they are here
 * and not repeated as figures on the page header. A number nobody can act on is a
 * number people stop reading, and the same number in two places is one people stop
 * trusting.
 *
 * Selection is held in the URL, not in this component, so a chip stays lit after a
 * reload, the back button steps through states, and a filtered view is a link
 * someone can paste. It writes only the `status` term, so a chip cannot wipe out
 * whatever the filters panel or the column row put there.
 */
export function MasterStatusChips({ resource }: { resource: string }) {
  const { state, apply } = useServerTable();
  const { counts, total, isLoading } = useStatusCounts(resource);

  // "business-units" is a path segment; "All business units" is a sentence.
  const plural = resource.replace(/-/g, ' ');

  const active = valuesFor(state.filter, ['status']).status ?? '';

  const select = (status: string) =>
    apply({
      filter: replaceOwnedTerms(
        state.filter,
        ['status'],
        // Clicking the lit chip clears it — the same control turns the filter off,
        // so there is no separate "show all" to hunt for.
        status && status !== active ? [{ field: 'status', operator: 'eq', value: status }] : [],
      ),
    });

  const chips: { id: string; label: string; count: number; tone: string }[] = [
    { id: '', label: `All ${plural}`, count: total, tone: 'text-foreground' },
    ...counts.map((entry) => ({
      id: entry.status,
      label: entry.label,
      count: entry.count,
      tone: STATUS_TEXT[entry.status] ?? 'text-foreground',
    })),
  ];

  return (
    <div
      role="group"
      aria-label="Filter by status"
      className="border-border bg-muted inline-flex shrink-0 gap-0.5 overflow-x-auto rounded-xl border p-1"
    >
      {chips.map((chip) => {
        const isOn = chip.id === active;

        return (
          <button
            key={chip.id || 'all'}
            type="button"
            aria-pressed={isOn}
            aria-label={`${chip.label}, ${chip.count} ${plural} — filter grid`}
            onClick={() => select(chip.id)}
            className={`flex h-11 flex-col justify-center rounded-lg px-3 text-left transition-colors ${
              isOn
                ? 'bg-card ring-line-strong shadow-sm ring-1 ring-inset'
                : 'hover:bg-accent bg-transparent'
            }`}
          >
            <span className="text-ink-faint text-[11px] leading-tight whitespace-nowrap">
              {chip.label}
            </span>
            {/* The count carries the state's colour. It used to be a separate dot,
                which had nothing to align to beside a two-line stack and read as
                debris; the number was already the largest thing on the chip, so
                colouring it says the same thing with one element fewer. The label
                above still names the state, so colour is reinforcement rather than
                the only way to tell two chips apart. */}
            <span className={`text-base leading-tight font-semibold tabular-nums ${chip.tone}`}>
              {/* An em dash while loading, not 0. Zero is a fact about the data
                  and would send someone looking for records that are simply not
                  counted yet. */}
              {isLoading ? '—' : chip.count.toLocaleString('en-IN')}
            </span>
          </button>
        );
      })}
    </div>
  );
}
