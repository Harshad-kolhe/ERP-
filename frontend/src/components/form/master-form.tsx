'use client';

import { ChevronLeft } from 'lucide-react';
import { useMemo, useState, type ReactNode } from 'react';
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
  /**
   * Width on the section's twelve-column grid, as Tailwind classes. Defaults to a
   * quarter — four fields to a row on a wide screen, which is the density the
   * approved design uses. `wide` is the shorthand for a full row.
   */
  span?: string;
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

/** A badge beside the record's key in the identity bar. */
export interface MasterFormBadge {
  label: string;
  tone?: 'neutral' | 'ok' | 'warn';
}

/**
 * The add and edit screen for every master.
 *
 * One page, top to bottom — not tabs. Each section is a numbered card carrying
 * its own issue count, so the whole record is legible by scrolling and a failed
 * save cannot hide a bad field behind a tab nobody opened. That is the design
 * signed off on the Part Master prototype, and this is a faithful port of it
 * rather than an adaptation.
 *
 * Sections and fields are data, not markup, for the same reason the import
 * columns are: the field list and its validation come from one declaration per
 * master, so a field cannot exist on the form and be missing from the payload.
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
  title,
  backLabel,
  identityCode,
  identityPlaceholder,
  badges = [],
  auditLine,
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
  /** Heading in the identity bar, e.g. "Edit part". */
  title: string;
  /** What the back button says it returns to, e.g. "Parts". */
  backLabel: string;
  /** The record's business key, shown monospaced. Absent on a new record. */
  identityCode?: string | null;
  /** Stands in for the key before one exists, e.g. "Assigned on save". */
  identityPlaceholder?: string;
  badges?: MasterFormBadge[];
  /** Created/modified line, right-aligned. Hidden on narrow screens. */
  auditLine?: string | null;
}) {
  const { errors, isDirty } = form.formState;

  // Joined rather than passed as an object: the memo below must re-run when the
  // *set* of failing fields changes, and the errors object is a new reference on
  // every render.
  const errorKeys = Object.keys(errors).sort().join(',');

  const errorCountBySection = useMemo(() => {
    const counts = new Map<string, number>();

    for (const section of sections) {
      counts.set(section.id, section.fields.filter((field) => Boolean(errors[field.name])).length);
    }

    return counts;
  }, [sections, errorKeys]); // eslint-disable-line react-hooks/exhaustive-deps

  const totalErrors = [...errorCountBySection.values()].reduce((sum, count) => sum + count, 0);

  /**
   * Discarding work asks first, and asks in the action bar rather than through
   * `window.confirm` — a native dialog cannot say which record it means and
   * cannot be styled to look like it belongs to this application.
   */
  const [confirm, setConfirm] = useState<'leave' | 'reset' | null>(null);

  const requestLeave = () => (isDirty ? setConfirm('leave') : onCancel());

  return (
    <Form {...form}>
      <form onSubmit={onSubmit} noValidate className="flex min-h-0 flex-1 flex-col">
        {/* ---------- one sticky bar: navigation, identity and state ---------- */}
        <header className="sticky top-0 z-30 flex flex-wrap items-center gap-x-4 gap-y-2 bg-[#003366] px-5 py-2.5 shadow-sm">
          <button
            type="button"
            onClick={requestLeave}
            className="inline-flex h-8 items-center gap-1 rounded-lg bg-white/10 px-2.5 text-xs font-medium text-white hover:bg-white/20"
          >
            <ChevronLeft className="h-3.5 w-3.5" /> {backLabel}
          </button>

          <h1 className="text-lg font-semibold tracking-tight text-white">{title}</h1>

          <div className="flex items-center gap-2 border-l border-white/20 pl-4">
            {identityCode ? (
              <span className="font-mono text-sm text-white/90">{identityCode}</span>
            ) : (
              <span className="font-mono text-sm text-white/70">
                {identityPlaceholder ?? 'Assigned on save'}
              </span>
            )}

            {badges.map((badge) => (
              <IdentityBadge key={badge.label} badge={badge} />
            ))}
          </div>

          <span className="flex-1" />

          {auditLine && <p className="hidden text-[11px] text-white/70 md:block">{auditLine}</p>}
        </header>

        <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3">
          <div className="mx-auto flex w-full max-w-[1400px] flex-col gap-4">
            {sections.map((section, index) => (
              <Section
                key={section.id}
                step={index + 1}
                title={section.label}
                description={section.description}
                errorCount={errorCountBySection.get(section.id) ?? 0}
              >
                {section.fields.map((field) => (
                  <FieldControl<TValues>
                    key={field.name}
                    field={field}
                    disabled={disabled || isSubmitting}
                    lookups={lookups}
                  />
                ))}
              </Section>
            ))}
          </div>
        </div>

        {/* ---------- action bar, pinned so Save is never below the fold ---------- */}
        <div className="border-line bg-surface/90 supports-[backdrop-filter]:bg-surface/75 sticky bottom-0 z-20 flex flex-wrap items-center gap-2 border-t px-4 py-3 backdrop-blur-md">
          {confirm ? (
            <div role="alertdialog" aria-label="Confirm" className="flex w-full flex-wrap items-center gap-2">
              <p className="text-ink text-[13px]">
                {confirm === 'leave'
                  ? 'Discard unsaved changes to this record?'
                  : 'Reset every field back to the last saved values?'}
              </p>
              <span className="flex-1" />
              <Button type="button" variant="outline" autoFocus onClick={() => setConfirm(null)}>
                Keep editing
              </Button>
              <Button
                type="button"
                variant="destructive"
                onClick={() => {
                  if (confirm === 'leave') onCancel();
                  else form.reset();
                  setConfirm(null);
                }}
              >
                Discard
              </Button>
            </div>
          ) : (
            <>
              <Button type="submit" disabled={isSubmitting || disabled}>
                {isSubmitting ? <Spinner className="mr-2 size-4" /> : null}
                {submitLabel}
              </Button>

              <div className="min-w-0 flex-1 px-2">
                <FormError message={formError} />
                {/* Counted across every section, so a problem further down the
                    page is visible from the bar without scrolling to find it. */}
                {!formError && totalErrors > 0 ? (
                  <p role="alert" className="text-destructive text-sm">
                    {totalErrors} field{totalErrors === 1 ? '' : 's'} need attention.
                  </p>
                ) : null}
              </div>

              {isDirty && <span className="text-ink-3 text-[11px]">Unsaved changes</span>}

              <Button
                type="button"
                variant="outline"
                onClick={() => setConfirm('reset')}
                disabled={isSubmitting || !isDirty}
              >
                Reset
              </Button>

              {/* The back button in the identity bar is the page's single exit,
                  and it runs the same unsaved-changes guard. */}
            </>
          )}
        </div>
      </form>
    </Form>
  );
}

function Section({
  step,
  title,
  description,
  errorCount,
  children,
}: {
  step: number;
  title: string;
  description?: string;
  errorCount: number;
  children: ReactNode;
}) {
  return (
    <section className="border-line bg-surface overflow-hidden rounded-xl border shadow-sm">
      <div className="border-line bg-surface-2 flex items-center gap-2 border-b px-4 py-2.5">
        <span className="bg-brand-soft text-brand-strong flex h-5 w-5 items-center justify-center rounded-full text-[11px] font-bold">
          {step}
        </span>
        <h2 className="text-ink text-[13px] font-semibold">{title}</h2>
        <span className="flex-1" />
        {errorCount > 0 && (
          <span className="text-destructive text-[11px] font-medium">
            {errorCount} issue{errorCount > 1 ? 's' : ''}
          </span>
        )}
      </div>

      {description && (
        <p className="text-ink-2 border-line border-b px-4 py-2 text-xs">{description}</p>
      )}

      {/* `contents` keeps fieldset semantics without drawing a second box. */}
      <fieldset className="contents">
        <div className="grid grid-cols-1 gap-x-4 gap-y-3 p-4 sm:grid-cols-6 lg:grid-cols-12">
          {children}
        </div>
      </fieldset>
    </section>
  );
}

function IdentityBadge({ badge }: { badge: MasterFormBadge }) {
  const tone =
    badge.tone === 'ok'
      ? 'border-emerald-300/40 bg-emerald-400/20 text-white'
      : badge.tone === 'warn'
        ? 'border-amber-300/40 bg-amber-400/20 text-white'
        : 'border-white/30 bg-white/10 text-white';

  return (
    <span className={`rounded-full border px-2 py-0.5 text-[11px] font-medium ${tone}`}>
      {badge.label}
    </span>
  );
}

/** A quarter of the row on a wide screen, a third on a medium one. */
const DEFAULT_SPAN = 'sm:col-span-3 lg:col-span-3';
const FULL_SPAN = 'sm:col-span-6 lg:col-span-12';

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

  // A textarea holding a specification or an address spans the row unless the
  // declaration says otherwise; everything else is a quarter.
  const span =
    field.span ?? (field.wide || field.kind === 'textarea' ? FULL_SPAN : DEFAULT_SPAN);

  const wrap = (children: ReactNode) => <div className={span}>{children}</div>;

  // A reference searches a master rather than rendering its rows as options.
  // Checked before `lookup` because the two are alternatives, not a stack.
  if (field.reference) {
    return wrap(
      <ReferenceField<TValues>
        {...common}
        source={field.reference}
        initialLabel={field.referenceLabel}
      />,
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

    return wrap(
      <SelectField<TValues>
        {...common}
        options={options}
        placeholder={options.length === 0 ? 'Loading…' : 'Select…'}
      />,
    );
  }

  switch (field.kind) {
    case 'textarea':
      return wrap(<TextareaField<TValues> {...common} rows={field.rows ?? 3} />);

    case 'boolean':
      return wrap(<CheckboxField<TValues> {...common} />);

    case 'number':
    case 'integer':
      // type="text" with a numeric inputmode: a number input silently drops what
      // it cannot parse as you type, so a half-entered "1." disappears.
      return wrap(<TextField<TValues> {...common} placeholder={field.placeholder} />);

    case 'date':
      return wrap(<TextField<TValues> {...common} type="date" />);

    default:
      return wrap(<TextField<TValues> {...common} placeholder={field.placeholder} />);
  }
}
