'use client';

import { ChevronRight, Filter, RotateCcw, Search } from 'lucide-react';
import { useState } from 'react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { NativeSelect } from '@/components/ui/native-select';

import { countActive, type FilterOperator } from './filter-terms';
import type { LookupSet } from './use-lookups';

/**
 * One field on a master's filters panel.
 *
 * Declared as data, exactly as the grid's columns and the form's sections are, so
 * a master's searchable fields are one array rather than a bespoke form. `field`
 * must name something the server declares on that endpoint's `QueryMap`, or the
 * request comes back 400.
 */
export interface MasterFilterField {
  field: string;
  label: string;
  /** Server-held option list — see `useLookups`. Makes the control a dropdown. */
  lookup?: string;
  /** Defaults to `contains` for free text; use `eq` for codes, flags and numbers. */
  operator?: FilterOperator;
  placeholder?: string;
}

/**
 * The filters panel that sits above every master grid.
 *
 * One component for all of them, configured by `fields`. The alternative — a
 * hand-built panel per master — is how the legacy system ended up with search
 * forms that disagreed about whether a blank box meant "all" or "blank", and it
 * is the thing this repository exists to avoid repeating.
 *
 * It keeps a *draft* separate from what is *applied*. The panel is a form: you
 * fill several boxes and press Search once, rather than firing a query per
 * keystroke across eight fields. The badge counts the applied filter, never the
 * draft — a panel that claims "3 applied" over an unfiltered grid is lying — and
 * a second badge appears when the draft and the applied filter have diverged, so
 * "I typed it but nothing happened" is visible rather than mysterious.
 *
 * It owns only the fields it declares. The grid's own column filter row owns the
 * rest, and the two share one query string through `replaceOwnedTerms`.
 */
export function MasterFilters({
  noun,
  fields,
  values,
  lookups,
  onApply,
  onReset,
  onClose,
}: {
  /** Names the panel: "Part filters". */
  noun: string;
  fields: readonly MasterFilterField[];
  /** What is filtering the grid right now, keyed by field. */
  values: Record<string, string>;
  lookups: LookupSet;
  onApply: (next: Record<string, string>) => void;
  onReset: () => void;
  onClose: () => void;
}) {
  const [draft, setDraft] = useState<Record<string, string>>(values);

  /**
   * Re-seed when the applied filter moves underneath the panel — the URL is the
   * source of truth, and it changes when someone follows a link, presses Back, or
   * clears a chip in the grid above.
   *
   * Adjusted during render rather than in an effect. An effect would let one
   * frame paint with the previous draft still in the boxes, and calling setState
   * inside one is the cascading re-render that `react-hooks/set-state-in-effect`
   * exists to catch.
   */
  const appliedKey = JSON.stringify(values);
  const [seededFrom, setSeededFrom] = useState(appliedKey);

  if (appliedKey !== seededFrom) {
    setSeededFrom(appliedKey);
    setDraft(values);
  }

  const appliedCount = countActive(values);
  const pending = JSON.stringify(normalise(draft, fields)) !== JSON.stringify(normalise(values, fields));

  return (
    <section aria-labelledby="master-filters-heading" className="border-line border-t">
      <div className="border-line flex items-center gap-2 border-b px-4 py-2">
        <Filter className="text-ink-3 h-3.5 w-3.5" />
        <h2
          id="master-filters-heading"
          className="text-ink-2 text-[11px] font-semibold tracking-wide uppercase"
        >
          {noun} filters
        </h2>

        {appliedCount > 0 && (
          <span className="border-primary/30 bg-primary/10 text-primary rounded-full border px-2 py-0.5 text-[11px] font-medium">
            {appliedCount} applied
          </span>
        )}

        {pending && (
          <span className="rounded-full border border-amber-500/40 bg-amber-500/15 px-2 py-0.5 text-[11px] font-medium text-amber-700 dark:text-amber-300">
            Unapplied changes
          </span>
        )}

        <span className="flex-1" />

        <button
          type="button"
          aria-label="Hide filters"
          onClick={onClose}
          className="text-ink-3 hover:text-ink rounded p-1"
        >
          <ChevronRight className="h-4 w-4 -rotate-90" />
        </button>
      </div>

      <form
        id="master-filters-panel"
        onSubmit={(event) => {
          event.preventDefault();
          onApply(draft);
        }}
        className="grid grid-cols-1 gap-3 p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        {fields.map((field) => {
          const id = `filter-${field.field}`;
          const options = field.lookup ? (lookups[field.lookup] ?? []) : null;

          return (
            <div key={field.field} className="grid gap-1.5">
              <label htmlFor={id} className="text-ink-2 text-xs font-medium">
                {field.label}
              </label>

              {options ? (
                <NativeSelect
                  id={id}
                  value={draft[field.field] ?? ''}
                  onChange={(event) =>
                    setDraft((previous) => ({ ...previous, [field.field]: event.target.value }))
                  }
                >
                  <option value="">All</option>
                  {options.map((option) => (
                    <option key={option.code} value={option.code}>
                      {option.name}
                    </option>
                  ))}
                </NativeSelect>
              ) : (
                <Input
                  id={id}
                  value={draft[field.field] ?? ''}
                  placeholder={field.placeholder ?? 'Contains…'}
                  onChange={(event) =>
                    setDraft((previous) => ({ ...previous, [field.field]: event.target.value }))
                  }
                />
              )}
            </div>
          );
        })}

        <div className="flex items-end gap-2 sm:col-span-2 lg:col-span-4">
          <Button type="submit" size="sm">
            <Search className="mr-1.5 h-3.5 w-3.5" />
            {pending ? 'Apply' : 'Search'}
          </Button>

          <Button type="button" size="sm" variant="outline" onClick={onReset}>
            <RotateCcw className="mr-1.5 h-3.5 w-3.5" />
            Reset
          </Button>

          <span className="text-ink-3 text-xs">
            {/* Says where the work happens. These screens exist because the system
                they replace pulled whole tables into the browser to search them. */}
            Filtering runs in the database, across every page — not just the rows on screen.
          </span>
        </div>
      </form>
    </section>
  );
}

/** The trigger, rendered in the grid's controls band. */
export function MasterFiltersTrigger({
  open,
  appliedCount,
  onToggle,
}: {
  open: boolean;
  appliedCount: number;
  onToggle: () => void;
}) {
  return (
    <button
      type="button"
      aria-expanded={open}
      aria-controls="master-filters-panel"
      onClick={onToggle}
      className={`border-line hover:border-line-strong focus-visible:ring-primary inline-flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-colors outline-none focus-visible:ring-2 ${
        appliedCount > 0 ? 'border-primary/30 bg-primary/10 text-primary' : 'bg-surface text-ink-2'
      }`}
    >
      <Filter className="h-3.5 w-3.5" />
      Filters
      {appliedCount > 0 && (
        <span className="bg-primary/15 text-primary rounded-full px-1.5 tabular-nums">
          {appliedCount}
        </span>
      )}
      <ChevronRight className={`h-3.5 w-3.5 transition-transform ${open ? 'rotate-90' : ''}`} />
    </button>
  );
}

/** Blank and absent are the same statement, so they must compare equal. */
function normalise(
  values: Record<string, string>,
  fields: readonly MasterFilterField[],
): Record<string, string> {
  const result: Record<string, string> = {};
  for (const field of fields) result[field.field] = (values[field.field] ?? '').trim();
  return result;
}

/** The lookup lists a panel needs, so the page can fetch them in one request. */
export function filterLookups(fields: readonly MasterFilterField[]): string[] {
  return [...new Set(fields.map((field) => field.lookup).filter((name): name is string => Boolean(name)))];
}
