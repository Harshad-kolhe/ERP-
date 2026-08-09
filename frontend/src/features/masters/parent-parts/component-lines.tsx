'use client';

import { useMemo } from 'react';

import { ReferencePicker, referenceSource } from '@/components/form/reference-field';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import type { PartListItem } from '@/lib/api/types';

/**
 * One line as the form holds it.
 *
 * Every number is a string, for the same reason the rest of the form's numbers
 * are: an input produces text, and coercing early turns a half-typed "1." into
 * NaN and an empty box into 0 — so a field nobody touched arrives at the server as
 * a real value.
 *
 * `key` is a client-side identity for React's list reconciliation, not the part id
 * and not a database key: two blank rows must stay distinguishable while the user
 * fills them in.
 */
export interface ComponentLine {
  key: string;
  partId: string;
  /** What the picker shows before anything is searched. */
  partLabel: string | null;
  quantity: string;
  unitOfMeasureCode: string;
  unitWeightKg: string;
  rate: string;
  drawingNumber: string;
  remark: string;
}

export function newComponentLine(): ComponentLine {
  return {
    key: crypto.randomUUID(),
    partId: '',
    partLabel: null,
    quantity: '1',
    unitOfMeasureCode: '',
    unitWeightKg: '',
    rate: '',
    drawingNumber: '',
    remark: '',
  };
}

/**
 * The component grid on the Parent Part form.
 *
 * A repeating editor rather than a set of tabs, because a build is read down a
 * column: people compare quantities and weights across lines, and a form that
 * shows one line at a time makes that impossible.
 *
 * Amount and line weight are shown but never edited — the server computes them
 * from quantity × rate and quantity × unit weight, and the header totals are
 * summed from those. The legacy screen accepted both from the browser, so a
 * hand-edited request could store a line whose amount disagreed with its own
 * quantity and rate. The figures here are a preview of what the server will
 * compute, recalculated as you type.
 */
export function ComponentLines({
  lines,
  onChange,
  disabled,
}: {
  lines: ComponentLine[];
  onChange: (lines: ComponentLine[]) => void;
  disabled?: boolean;
}) {
  const partSource = useMemo(
    () =>
      referenceSource<PartListItem>({
        resource: 'parts',
        // Only parts still in use can be added to a build.
        filter: 'isActive:eq:true',
        searchPlaceholder: 'Search part number or description…',
        toOption: (row) => ({ value: row.id, label: row.partNumber, hint: row.description }),
      }),
    [],
  );

  const totals = useMemo(() => {
    let weight = 0;
    let amount = 0;

    for (const line of lines) {
      const quantity = toNumber(line.quantity);
      weight += quantity * toNumber(line.unitWeightKg);
      amount += quantity * toNumber(line.rate);
    }

    return { weight, amount };
  }, [lines]);

  function update(key: string, patch: Partial<ComponentLine>) {
    onChange(lines.map((line) => (line.key === key ? { ...line, ...patch } : line)));
  }

  function remove(key: string) {
    onChange(lines.filter((line) => line.key !== key));
  }

  // Flagged in the editor rather than only on save: the server rejects a duplicate
  // part outright, and finding out which line at submit time is worse than being
  // told while adding it.
  const duplicateKeys = useMemo(() => {
    const seen = new Map<string, string>();
    const duplicates = new Set<string>();

    for (const line of lines) {
      if (!line.partId) continue;
      const first = seen.get(line.partId);

      if (first) {
        duplicates.add(first);
        duplicates.add(line.key);
      } else {
        seen.set(line.partId, line.key);
      }
    }

    return duplicates;
  }, [lines]);

  return (
    <div className="flex min-h-0 flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-ink-2 text-sm">
          {lines.length === 0
            ? 'No components yet. A build can be saved empty and filled in later.'
            : `${lines.length} component line${lines.length === 1 ? '' : 's'}.`}
        </p>

        <Button
          type="button"
          size="sm"
          variant="outline"
          disabled={disabled}
          onClick={() => onChange([...lines, newComponentLine()])}
        >
          Add component
        </Button>
      </div>

      {lines.length > 0 ? (
        <div className="border-line overflow-x-auto rounded-lg border">
          <table className="w-full min-w-[1100px] text-sm">
            <thead className="bg-surface-2 text-ink-2">
              <tr className="[&>th]:px-3 [&>th]:py-2 [&>th]:text-left [&>th]:font-medium">
                <th className="w-10">#</th>
                <th className="min-w-[260px]">Component part</th>
                <th className="w-28 text-right">Qty</th>
                <th className="w-24">UOM</th>
                <th className="w-32 text-right">Unit wt (kg)</th>
                <th className="w-32 text-right">Rate</th>
                <th className="w-32 text-right">Line wt</th>
                <th className="w-32 text-right">Amount</th>
                <th className="w-40">Drawing</th>
                <th className="w-12" />
              </tr>
            </thead>

            <tbody>
              {lines.map((line, index) => {
                const quantity = toNumber(line.quantity);
                const lineWeight = quantity * toNumber(line.unitWeightKg);
                const amount = quantity * toNumber(line.rate);
                const isDuplicate = duplicateKeys.has(line.key);

                return (
                  <tr
                    key={line.key}
                    className={`border-line border-t align-top [&>td]:px-3 [&>td]:py-2 ${
                      isDuplicate ? 'bg-destructive/5' : ''
                    }`}
                  >
                    <td className="text-ink-3 pt-4 tabular-nums">{index + 1}</td>

                    <td>
                      <ReferencePicker
                        value={line.partId}
                        ariaLabel={`Component part, line ${index + 1}`}
                        disabled={disabled}
                        source={partSource}
                        initialLabel={line.partLabel}
                        onChange={(value, option) =>
                          update(line.key, {
                            partId: value,
                            partLabel: option
                              ? `${option.label} — ${option.hint ?? ''}`.replace(/—\s*$/, '').trim()
                              : null,
                          })
                        }
                      />
                      {isDuplicate ? (
                        <p role="alert" className="text-destructive mt-1 text-xs">
                          This part is on the build twice. Change its quantity instead.
                        </p>
                      ) : null}
                    </td>

                    <NumberCell
                      value={line.quantity}
                      label={`Quantity, line ${index + 1}`}
                      disabled={disabled}
                      onChange={(value) => update(line.key, { quantity: value })}
                    />

                    <td>
                      <Input
                        aria-label={`Unit of measure, line ${index + 1}`}
                        disabled={disabled}
                        value={line.unitOfMeasureCode}
                        placeholder="—"
                        onChange={(event) =>
                          update(line.key, { unitOfMeasureCode: event.target.value })
                        }
                      />
                    </td>

                    <NumberCell
                      value={line.unitWeightKg}
                      label={`Unit weight, line ${index + 1}`}
                      disabled={disabled}
                      onChange={(value) => update(line.key, { unitWeightKg: value })}
                    />

                    <NumberCell
                      value={line.rate}
                      label={`Rate, line ${index + 1}`}
                      disabled={disabled}
                      onChange={(value) => update(line.key, { rate: value })}
                    />

                    {/* Computed, and shown as such: no input, no border, nothing to
                        type into. The server recomputes them on save regardless. */}
                    <td className="text-ink-2 pt-4 text-right tabular-nums">
                      {format(lineWeight, 4)}
                    </td>
                    <td className="text-ink-2 pt-4 text-right tabular-nums">
                      {format(amount, 2)}
                    </td>

                    <td>
                      <Input
                        aria-label={`Drawing number, line ${index + 1}`}
                        disabled={disabled}
                        value={line.drawingNumber}
                        placeholder="—"
                        onChange={(event) => update(line.key, { drawingNumber: event.target.value })}
                      />
                    </td>

                    <td className="pt-3 text-right">
                      <button
                        type="button"
                        disabled={disabled}
                        onClick={() => remove(line.key)}
                        aria-label={`Remove line ${index + 1}`}
                        className="text-ink-3 hover:text-destructive rounded-md px-1.5 py-1 text-sm disabled:opacity-40"
                      >
                        ✕
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>

            <tfoot className="border-line bg-surface-2 border-t">
              <tr className="[&>td]:px-3 [&>td]:py-2">
                <td colSpan={6} className="text-ink-2 text-right font-medium">
                  Totals
                </td>
                <td className="text-right font-medium tabular-nums">{format(totals.weight, 4)}</td>
                <td className="text-right font-medium tabular-nums">{format(totals.amount, 2)}</td>
                <td colSpan={2} />
              </tr>
            </tfoot>
          </table>
        </div>
      ) : null}
    </div>
  );
}

/** A right-aligned numeric cell. Text, not `type="number"` — see `ComponentLine`. */
function NumberCell({
  value,
  label,
  disabled,
  onChange,
}: {
  value: string;
  label: string;
  disabled?: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <td>
      <Input
        aria-label={label}
        inputMode="decimal"
        className="text-right"
        disabled={disabled}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </td>
  );
}

function toNumber(value: string): number {
  const parsed = Number(value.trim());
  return Number.isFinite(parsed) ? parsed : 0;
}

function format(value: number, decimals: number): string {
  return value.toLocaleString('en-IN', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
}
