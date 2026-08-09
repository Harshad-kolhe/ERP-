'use client';

import { useEffect, useMemo, useState } from 'react';
import type { FieldPath, FieldValues, UseFormReturn } from 'react-hook-form';

import {
  CheckboxField,
  FormError,
  SelectField,
  TextField,
  TextareaField,
} from '@/components/form/fields';
import { ReferenceField, type ReferenceSource } from '@/components/form/reference-field';
import { Button } from '@/components/ui/button';
import { Form } from '@/components/ui/form';
import { Spinner } from '@/components/ui/spinner';

/**
 * Note the absence of a `select`. A dropdown is not a kind of field here — it is
 * a field with a `lookup`, and the options come from the server. Offering a
 * `select` that carried its own options would be the one door through which a
 * hardcoded list could get back in.
 */
export type MasterFieldKind = 'text' | 'textarea' | 'number' | 'integer' | 'date' | 'boolean';

export interface MasterField<TValues extends FieldValues> {
  name: FieldPath<TValues>;
  label: string;
  kind?: MasterFieldKind;
  required?: boolean;
  description?: string;
  placeholder?: string;
  /**
   * Name of a server-held option list — see `useLookups`. This is how a field
   * becomes a dropdown: the options are fetched, never written here. A field with
   * a lookup renders as a select whether or not `kind` says so.
   */
  lookup?: string;
  /**
   * Makes the field a picker over another master, searched on the server. Use it
   * when the choices are rows of a table rather than a short list of codes — a
   * lookup renders every option, and a part master has thousands.
   */
  reference?: ReferenceSource;
  /**
   * What a `reference` field shows before anything is searched, when it already
   * has a value. Detail DTOs send the referenced record's code and name with the
   * id precisely so this costs no extra request.
   */
  referenceLabel?: string | null;
  /** Span the full row. Use for addresses and specifications. */
  wide?: boolean;
  rows?: number;
  /** Shown but not editable — a business key on an existing record. */
  readOnly?: boolean;
}

export interface MasterFormSection<TValues extends FieldValues> {
  id: string;
  label: string;
  description?: string;
  fields: MasterField<TValues>[];
}

/**
 * The add and edit screen for every master.
 *
 * Master records here run to 37 fields, and the two ways of showing that many are
 * both bad on their own: one page is a scroll nobody reads to the bottom of, and
 * tabs hide the field that is stopping the save. So this is tabbed, and the tabs
 * carry their own error counts — a failed submit tells you *which group* to look
 * in, and jumps you there if you are not already in one that needs attention.
 *
 * Sections are data, not markup, for the same reason the import columns are: the
 * field list and its validation come from one declaration per master, so a field
 * cannot exist on the form and be missing from the payload.
 *
 * Every field stays registered when its tab is hidden — react-hook-form keeps
 * unmounted values by default — so the schema validates the whole record no matter
 * which tab is open, and Save works from any of them.
 */
export function MasterForm<TValues extends FieldValues>({
  sections,
  form,
  onSubmit,
  isSubmitting,
  formError,
  submitLabel,
  onCancel,
  disabled = false,
  lookups = {},
}: {
  sections: MasterFormSection<TValues>[];
  form: UseFormReturn<TValues, unknown, TValues>;
  onSubmit: (event: React.FormEvent) => void;
  isSubmitting: boolean;
  formError: string | null;
  submitLabel: string;
  onCancel: () => void;
  disabled?: boolean;
  /** Server-held option lists, keyed by lookup name. See `useLookups`. */
  lookups?: Record<string, { code: string; name: string }[]>;
}) {
  const [activeId, setActiveId] = useState(sections[0]?.id ?? '');

  const { errors, submitCount } = form.formState;

  // Joined rather than passed as an object: the effect below must re-run when the
  // *set* of failing fields changes, and the errors object is a new reference on
  // every render.
  const errorKeys = Object.keys(errors).sort().join(',');

  const errorCountBySection = useMemo(() => {
    const counts = new Map<string, number>();

    for (const section of sections) {
      counts.set(
        section.id,
        section.fields.filter((field) => Boolean(errors[field.name])).length,
      );
    }

    return counts;
  }, [sections, errorKeys]); // eslint-disable-line react-hooks/exhaustive-deps

  /**
   * Move to the first section that has a problem — but only when the section the
   * user is looking at has none. Yanking someone off a tab they are still fixing
   * is worse than leaving them to work through it.
   */
  useEffect(() => {
    if (!errorKeys) {
      return;
    }

    if ((errorCountBySection.get(activeId) ?? 0) > 0) {
      return;
    }

    const firstFailing = sections.find((section) => (errorCountBySection.get(section.id) ?? 0) > 0);

    if (firstFailing) {
      setActiveId(firstFailing.id);
    }
  }, [errorKeys, submitCount, activeId, errorCountBySection, sections]);

  const active = sections.find((section) => section.id === activeId) ?? sections[0];
  const totalErrors = [...errorCountBySection.values()].reduce((sum, count) => sum + count, 0);

  return (
    <Form {...form}>
      <form onSubmit={onSubmit} noValidate className="flex min-h-0 flex-1 flex-col">
        {sections.length > 1 ? (
          <div
            role="tablist"
            aria-label="Form sections"
            className="border-line flex flex-wrap gap-1 border-b px-6"
          >
            {sections.map((section) => {
              const count = errorCountBySection.get(section.id) ?? 0;
              const selected = section.id === active?.id;

              return (
                <button
                  key={section.id}
                  type="button"
                  role="tab"
                  aria-selected={selected}
                  aria-controls={`section-${section.id}`}
                  onClick={() => setActiveId(section.id)}
                  className={`-mb-px flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
                    selected
                      ? 'border-primary text-ink'
                      : 'text-ink-2 hover:text-ink border-transparent'
                  }`}
                >
                  {section.label}
                  {count > 0 ? (
                    <span
                      className="bg-destructive/15 text-destructive rounded-full px-1.5 text-[11px] font-semibold tabular-nums"
                      aria-label={`${count} field${count === 1 ? '' : 's'} need attention`}
                    >
                      {count}
                    </span>
                  ) : null}
                </button>
              );
            })}
          </div>
        ) : null}

        <div className="min-h-0 flex-1 overflow-y-auto p-6">
          {active?.description ? (
            <p className="text-ink-2 mb-4 max-w-2xl text-sm">{active.description}</p>
          ) : null}

          <div
            id={`section-${active?.id}`}
            role="tabpanel"
            className="grid max-w-4xl gap-4 sm:grid-cols-2"
          >
            {active?.fields.map((field) => (
              <FieldControl<TValues>
                key={field.name}
                field={field}
                disabled={disabled || isSubmitting}
                lookups={lookups}
              />
            ))}
          </div>
        </div>

        <div className="border-line bg-surface flex items-center justify-between gap-3 border-t px-6 py-3">
          <div className="min-w-0 flex-1">
            <FormError message={formError} />
            {/* Counted across every tab, so a hidden problem is still visible. */}
            {!formError && totalErrors > 0 ? (
              <p role="alert" className="text-destructive text-sm">
                {totalErrors} field{totalErrors === 1 ? '' : 's'} need attention.
              </p>
            ) : null}
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || disabled}>
              {isSubmitting ? <Spinner className="mr-2 size-4" /> : null}
              {submitLabel}
            </Button>
          </div>
        </div>
      </form>
    </Form>
  );
}

/** Maps a field declaration onto the shared field vocabulary. */
function FieldControl<TValues extends FieldValues>({
  field,
  disabled,
  lookups,
}: {
  field: MasterField<TValues>;
  disabled: boolean;
  lookups: Record<string, { code: string; name: string }[]>;
}) {
  const common = {
    name: field.name,
    label: field.label,
    description: field.description,
    required: field.required,
    disabled: disabled || field.readOnly,
  };

  // A reference searches a master rather than rendering its rows as options.
  // Checked before `lookup` because the two are alternatives, not a stack.
  if (field.reference) {
    return (
      <div className={field.wide ? 'sm:col-span-2' : undefined}>
        <ReferenceField<TValues>
          {...common}
          source={field.reference}
          initialLabel={field.referenceLabel}
        />
      </div>
    );
  }

  // A lookup makes the field a dropdown regardless of `kind`. The options are
  // whatever the server sent — the client never supplies a fallback list, because
  // a fallback is a hardcoded list that only shows up when something is wrong.
  if (field.lookup) {
    const options = (lookups[field.lookup] ?? []).map((option) => ({
      value: option.code,
      label: option.name,
    }));

    return (
      <div className={field.wide ? 'sm:col-span-2' : undefined}>
        <SelectField<TValues>
          {...common}
          options={options}
          placeholder={options.length === 0 ? 'Loading…' : 'Select…'}
        />
      </div>
    );
  }

  switch (field.kind) {
    case 'textarea':
      return (
        <div className={field.wide === false ? undefined : 'sm:col-span-2'}>
          <TextareaField<TValues> {...common} rows={field.rows ?? 3} />
        </div>
      );

    case 'boolean':
      return (
        <div className={field.wide ? 'sm:col-span-2' : undefined}>
          <CheckboxField<TValues> {...common} />
        </div>
      );

    case 'number':
    case 'integer':
      return (
        <div className={field.wide ? 'sm:col-span-2' : undefined}>
          {/* type="text" with a numeric inputmode: a number input silently drops
              what it cannot parse as you type, so a half-entered "1." disappears. */}
          <TextField<TValues> {...common} placeholder={field.placeholder} />
        </div>
      );

    case 'date':
      return (
        <div className={field.wide ? 'sm:col-span-2' : undefined}>
          <TextField<TValues> {...common} type="date" />
        </div>
      );

    default:
      return (
        <div className={field.wide ? 'sm:col-span-2' : undefined}>
          <TextField<TValues> {...common} placeholder={field.placeholder} />
        </div>
      );
  }
}
