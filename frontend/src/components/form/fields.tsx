'use client';

import type { FieldPath, FieldValues } from 'react-hook-form';

import { Checkbox } from '@/components/ui/checkbox';
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { NativeSelect } from '@/components/ui/native-select';
import { Textarea } from '@/components/ui/textarea';

/**
 * The field vocabulary every form is written in.
 *
 * Each one renders a label, the control, an optional hint and the error in the same
 * arrangement, so ~180 screens agree on where an error message appears without any
 * of them deciding. `FormMessage` shows whichever error is present — Zod's on the
 * client, or the server's once `useApiForm` has attached it.
 */

interface BaseProps<TValues extends FieldValues> {
  name: FieldPath<TValues>;
  label: string;
  description?: string;
  required?: boolean;
  disabled?: boolean;
}

function RequiredMark({ required }: { required?: boolean }) {
  // A marker rather than the word "optional" on everything else: most fields on a
  // master record are optional, so marking the few that are not is far less noise.
  return required ? <span className="text-destructive ml-0.5">*</span> : null;
}

export function TextField<TValues extends FieldValues>({
  name,
  label,
  description,
  required,
  disabled,
  type = 'text',
  placeholder,
  autoComplete,
  className,
  inputMode,
}: BaseProps<TValues> & {
  type?: 'text' | 'email' | 'password' | 'number' | 'date';
  placeholder?: string;
  autoComplete?: string;
  className?: string;
  /**
   * Which on-screen keyboard a touch device should offer. Numeric fields here are
   * `type="text"` on purpose — a number input silently discards what it cannot
   * parse, so a half-typed "1." vanishes — which means the keyboard has to be
   * asked for separately rather than coming with the type.
   */
  inputMode?: React.ComponentProps<'input'>['inputMode'];
}) {
  return (
    <FormField<TValues>
      name={name}
      render={({ field }) => (
        <FormItem className={className} hasDescription={Boolean(description)}>
          <FormLabel>
            {label}
            <RequiredMark required={required} />
          </FormLabel>
          <FormControl>
            <Input
              type={type}
              placeholder={placeholder}
              autoComplete={autoComplete}
              inputMode={inputMode}
              disabled={disabled}
              {...field}
              value={field.value ?? ''}
            />
          </FormControl>
          {description ? <FormDescription>{description}</FormDescription> : null}
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export function TextareaField<TValues extends FieldValues>({
  name,
  label,
  description,
  required,
  disabled,
  rows = 3,
}: BaseProps<TValues> & { rows?: number }) {
  return (
    <FormField<TValues>
      name={name}
      render={({ field }) => (
        <FormItem hasDescription={Boolean(description)}>
          <FormLabel>
            {label}
            <RequiredMark required={required} />
          </FormLabel>
          <FormControl>
            <Textarea rows={rows} disabled={disabled} {...field} value={field.value ?? ''} />
          </FormControl>
          {description ? <FormDescription>{description}</FormDescription> : null}
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export function SelectField<TValues extends FieldValues>({
  name,
  label,
  description,
  required,
  disabled,
  options,
  placeholder = 'Select…',
}: BaseProps<TValues> & {
  options: { value: string; label: string }[];
  placeholder?: string;
}) {
  return (
    <FormField<TValues>
      name={name}
      render={({ field }) => (
        <FormItem hasDescription={Boolean(description)}>
          <FormLabel>
            {label}
            <RequiredMark required={required} />
          </FormLabel>
          <FormControl>
            <NativeSelect disabled={disabled} {...field} value={field.value ?? ''}>
              <option value="">{placeholder}</option>
              {options.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </NativeSelect>
          </FormControl>
          {description ? <FormDescription>{description}</FormDescription> : null}
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

/**
 * What an empty dropdown should say.
 *
 * Three states, not two: options present, options still coming, and options that
 * are never coming because the request failed. Collapsing the last two leaves a
 * form full of selects reading "Loading…" indefinitely, which sends the user
 * looking for reference data rather than telling them the call broke.
 */
export function selectPlaceholder(options: unknown[], failed: boolean): string {
  if (options.length > 0) return 'Select…';
  return failed ? 'Options unavailable' : 'Loading…';
}

export function CheckboxField<TValues extends FieldValues>({
  name,
  label,
  description,
  disabled,
}: BaseProps<TValues>) {
  return (
    <FormField<TValues>
      name={name}
      render={({ field }) => (
        <FormItem className="flex-row items-start gap-2.5 space-y-0" hasDescription={Boolean(description)}>
          <FormControl>
            <Checkbox
              checked={!!field.value}
              onCheckedChange={field.onChange}
              disabled={disabled}
              className="mt-0.5"
            />
          </FormControl>
          <div className="grid gap-1">
            <FormLabel className="font-normal">{label}</FormLabel>
            {description ? <FormDescription>{description}</FormDescription> : null}
            <FormMessage />
          </div>
        </FormItem>
      )}
    />
  );
}

/**
 * Form-level failure: a conflict, a server fault, or a field error with no matching
 * input. Rendered above the actions so it is never scrolled past.
 */
export function FormError({ message }: { message: string | null }) {
  if (!message) {
    return null;
  }

  return (
    <p
      role="alert"
      className="border-destructive/30 bg-destructive/10 text-destructive rounded-md border px-3 py-2 text-sm"
    >
      {message}
    </p>
  );
}
