'use client';

import { useServerTable } from '@/components/data-table/use-server-table';
import { replaceOwnedTerms, valuesFor } from '../shared/filter-terms';
import { usePartCounts } from '@/features/dashboard/use-dashboard';
import type { PartStatus } from '@/lib/api/types';

/**
 * The status band above the Part Master grid: how many parts are in each state,
 * and one click to see them.
 *
 * This is where the counts live. The prototype puts them here rather than in a
 * corner of the grid footer, and the reason is that they are not decoration — each
 * one is the filter for that state. A number nobody can act on is a number people
 * stop reading.
 *
 * Selection is held in the URL, not in this component, so a chip stays lit after a
 * reload, the back button steps through states, and a filtered view is a link
 * someone can paste. It writes only the `status` term, so a chip cannot wipe out
 * whatever the filters panel or the column row put there.
 */
export function PartStatusChips() {
  const { state, apply } = useServerTable();
  const { counts, total, isLoading } = usePartCounts();

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

  const chips: { id: string; label: string; count: number; dot: string }[] = [
    { id: '', label: 'All parts', count: total, dot: 'bg-ink-3' },
    ...counts.map((entry) => ({
      id: entry.status,
      label: entry.label,
      count: entry.count,
      dot: DOT[entry.status],
    })),
  ];

  return (
    <div
      role="group"
      aria-label="Filter by status"
      className="border-line bg-surface-2 inline-flex shrink-0 gap-0.5 overflow-x-auto rounded-xl border p-1"
    >
      {chips.map((chip) => {
        const isOn = chip.id === active;

        return (
          <button
            key={chip.id || 'all'}
            type="button"
            aria-pressed={isOn}
            aria-label={`${chip.label}, ${chip.count} parts — filter grid`}
            onClick={() => select(chip.id)}
            className={`flex h-11 items-center gap-2 rounded-lg px-3 text-left transition-colors ${
              isOn
                ? 'bg-surface text-ink ring-line-strong shadow-sm ring-1 ring-inset'
                : 'text-ink-2 hover:bg-surface-3 bg-transparent'
            }`}
          >
            <span aria-hidden="true" className={`h-2 w-2 shrink-0 rounded-full ${chip.dot}`} />
            <span className="flex flex-col">
              <span className="text-ink-3 text-[11px] leading-tight whitespace-nowrap">
                {chip.label}
              </span>
              <span className="text-ink text-base leading-tight font-semibold tabular-nums">
                {/* An em dash while loading, not 0. Zero is a fact about the data
                    and would send someone looking for parts that are simply not
                    counted yet. */}
                {isLoading ? '—' : chip.count.toLocaleString('en-IN')}
              </span>
            </span>
          </button>
        );
      })}
    </div>
  );
}

/** One source for the status colours, so a chip cannot disagree with the grid's pill. */
const DOT: Record<PartStatus, string> = {
  Draft: 'bg-ink-3',
  PendingApproval: 'bg-amber-500',
  Approved: 'bg-emerald-500',
  Rejected: 'bg-rose-500',
  Hold: 'bg-sky-500',
};
