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
}: BaseProps<TValues> & {
  type?: 'text' | 'email' | 'password' | 'number' | 'date';
  placeholder?: string;
  autoComplete?: string;
  className?: string;
}) {
  return (
    <FormField<TValues>
      name={name}
      render={({ field }) => (
        <FormItem className={className}>
          <FormLabel>
            {label}
            <RequiredMark required={required} />
          </FormLabel>
          <FormControl>
            <Input
              type={type}
              placeholder={placeholder}
              autoComplete={autoComplete}
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
        <FormItem>
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
        <FormItem>
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
        <FormItem className="flex-row items-start gap-2.5 space-y-0">
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
