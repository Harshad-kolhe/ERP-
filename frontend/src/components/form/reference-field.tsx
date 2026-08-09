'use client';

import { useQuery } from '@tanstack/react-query';
import { X } from 'lucide-react';
import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import type { FieldPath, FieldValues } from 'react-hook-form';

import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { apiFetch } from '@/lib/api/fetcher';
import type { PagedResult } from '@/lib/api/types';
import { cn } from '@/lib/utils';

/**
 * How a field finds the record it points at.
 *
 * Declared as data, like the rest of the form, and deliberately *not* a list of
 * options: the thing being chosen is a row of a master with thousands of rows, so
 * the only correct answer is to ask the server. The legacy screens loaded the
 * whole part master into a `<select>` and searched it in the browser — which is
 * the same failure as the grids that downloaded every row to page them.
 */
export interface ReferenceSource {
  /** Path segment under `/masters`, e.g. `parts` or `sections`. */
  resource: string;
  /** Server-side `filter=` applied to every query, e.g. `isActive:eq:true`. */
  filter?: string;
  /** Turns one row of that resource into what the user picks. */
  toOption: (row: unknown) => ReferenceOption;
  /** Placeholder for the search box. */
  searchPlaceholder?: string;
}

export interface ReferenceOption {
  /** Stored in the form field — an id, never a display string. */
  value: string;
  /** The line the user reads: a code or number. */
  label: string;
  /** Second line: a description, so two similar codes are distinguishable. */
  hint?: string;
}

/**
 * Builds a {@link ReferenceSource} with the row type known at the declaration
 * site, so `toOption` is checked against the DTO instead of taking `unknown`.
 */
export function referenceSource<TRow>(source: {
  resource: string;
  filter?: string;
  toOption: (row: TRow) => ReferenceOption;
  searchPlaceholder?: string;
}): ReferenceSource {
  return {
    resource: source.resource,
    filter: source.filter,
    searchPlaceholder: source.searchPlaceholder,
    toOption: (row) => source.toOption(row as TRow),
  };
}

/** How many matches the dropdown shows. Beyond this, the answer is "type more". */
const PAGE_SIZE = 20;

/** Milliseconds of quiet before a keystroke becomes a request. */
const DEBOUNCE_MS = 250;

/**
 * A field whose value is another record's id, chosen by searching the server.
 *
 * It holds two pieces of state that are easy to confuse: the *value* (an id, which
 * is what gets submitted) and the *search text* (what the user is typing, which is
 * never submitted). Conflating them is how a picker ends up posting whatever was
 * left in the box.
 *
 * `initialLabel` exists because an edit screen starts with an id and no label, and
 * fetching one record to render its name is a request that the detail endpoint has
 * already made — every detail DTO here sends the referenced code and name with it.
 */
export function ReferenceField<TValues extends FieldValues>({
  name,
  label,
  description,
  required,
  disabled,
  source,
  initialLabel,
  emptyHint = 'No matches.',
}: {
  name: FieldPath<TValues>;
  label: string;
  description?: string;
  required?: boolean;
  disabled?: boolean;
  source: ReferenceSource;
  /** What to show when the field already has a value and nothing has been searched yet. */
  initialLabel?: string | null;
  emptyHint?: string;
}) {
  return (
    <FormField<TValues>
      name={name}
      render={({ field }) => (
        <FormItem hasDescription={Boolean(description)}>
          <FormLabel>
            {label}
            {required ? <span className="text-destructive ml-0.5">*</span> : null}
          </FormLabel>
          <FormControl>
            <ReferencePicker
              value={(field.value as string | null) ?? ''}
              onChange={field.onChange}
              onBlur={field.onBlur}
              disabled={disabled}
              source={source}
              initialLabel={initialLabel}
              emptyHint={emptyHint}
            />
          </FormControl>
          {description ? <FormDescription>{description}</FormDescription> : null}
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

/**
 * The control itself, without the form wiring — usable directly inside a repeating
 * row such as the parent-part component grid, where there is no single field name.
 */
export function ReferencePicker({
  value,
  onChange,
  onBlur,
  disabled,
  source,
  initialLabel,
  emptyHint = 'No matches.',
  className,
  ariaLabel,
}: {
  value: string;
  onChange: (value: string, option: ReferenceOption | null) => void;
  onBlur?: () => void;
  disabled?: boolean;
  source: ReferenceSource;
  initialLabel?: string | null;
  emptyHint?: string;
  className?: string;
  ariaLabel?: string;
}) {
  const listId = useId();
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [debounced, setDebounced] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);

  /**
   * What the user picked in *this* component, remembered with the id it belongs
   * to.
   *
   * Storing the pair rather than a bare label is what lets the displayed label be
   * derived instead of synchronised. The id can change underneath us — an edit
   * screen finishes loading, or the form resets — and a lone label would then keep
   * describing the previous record until an effect corrected it, one paint later.
   * Here a stale pair simply stops matching and the caller's `initialLabel` takes
   * over in the same render.
   */
  const [picked, setPicked] = useState<{ value: string; label: string } | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLUListElement>(null);

  const chosenLabel = picked?.value === value ? picked.label : (initialLabel ?? null);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(search.trim()), DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [search]);

  /**
   * Where to draw the list, in viewport coordinates.
   *
   * The list is portalled to the body rather than positioned inside this
   * component, because the component is used inside the parent-part component
   * grid, whose `overflow-x-auto` wrapper clips absolutely-positioned descendants
   * on *both* axes — `overflow-x: auto` computes `overflow-y` to `auto` too. The
   * effect was that the primary control on that screen dropped a list that was cut
   * off a few pixels below the input.
   *
   * Re-measured on scroll with capture, so a scroll in any ancestor moves it and
   * not just a scroll of the window.
   */
  const [anchor, setAnchor] = useState<DOMRect | null>(null);

  useEffect(() => {
    if (!open) return;

    const measure = () => setAnchor(containerRef.current?.getBoundingClientRect() ?? null);

    measure();
    window.addEventListener('resize', measure);
    window.addEventListener('scroll', measure, true);

    return () => {
      window.removeEventListener('resize', measure);
      window.removeEventListener('scroll', measure, true);
    };
  }, [open]);

  // Clicking anywhere else closes the list. Blur alone is not enough: moving from
  // the input to an option is a blur, and closing on it would cancel the click.
  // The list is checked separately from the container — portalled, it is no longer
  // a descendant, so `container.contains` alone would treat every click on an
  // option as a click outside and tear the list down before it landed.
  useEffect(() => {
    if (!open) return;

    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node;
      if (containerRef.current?.contains(target) || listRef.current?.contains(target)) return;
      setOpen(false);
    };

    document.addEventListener('pointerdown', onPointerDown);
    return () => document.removeEventListener('pointerdown', onPointerDown);
  }, [open]);

  const query = useQuery({
    queryKey: ['masters', source.resource, 'reference', source.filter ?? '', debounced],
    queryFn: () => {
      const params = new URLSearchParams({ page: '1', pageSize: String(PAGE_SIZE) });
      if (debounced) params.set('search', debounced);
      if (source.filter) params.set('filter', source.filter);

      return apiFetch<PagedResult<unknown>>(`/masters/${source.resource}?${params}`);
    },
    // Only while the list is open. A closed picker on a form with ten of them
    // would otherwise fetch ten result sets nobody is looking at.
    enabled: open && !disabled,
    placeholderData: (previous) => previous,
    staleTime: 30 * 1000,
  });

  const options = useMemo(
    () => (query.data?.items ?? []).map(source.toOption),
    [query.data, source],
  );

  const total = query.data?.totalCount ?? 0;

  function choose(option: ReferenceOption) {
    setPicked({
      value: option.value,
      label: option.hint ? `${option.label} — ${option.hint}` : option.label,
    });

    onChange(option.value, option);
    setSearch('');
    setOpen(false);
  }

  function clear() {
    setPicked(null);
    onChange('', null);
    setSearch('');
  }

  return (
    <div ref={containerRef} className={cn('relative', className)}>
      <div className="flex items-center gap-1.5">
        <Input
          role="combobox"
          aria-expanded={open}
          aria-controls={listId}
          aria-autocomplete="list"
          aria-label={ariaLabel}
          autoComplete="off"
          disabled={disabled}
          placeholder={source.searchPlaceholder ?? 'Search…'}
          // Shows the selection until the user starts typing, then shows what they
          // are typing. Two states, one box — which is what a combobox is.
          value={open ? search : (chosenLabel ?? '')}
          onFocus={() => setOpen(true)}
          onBlur={onBlur}
          onChange={(event) => {
            setSearch(event.target.value);
            setActiveIndex(0);
            setOpen(true);
          }}
          onKeyDown={(event) => {
            if (event.key === 'ArrowDown') {
              event.preventDefault();
              setOpen(true);
              setActiveIndex((index) => Math.min(index + 1, Math.max(options.length - 1, 0)));
            } else if (event.key === 'ArrowUp') {
              event.preventDefault();
              setActiveIndex((index) => Math.max(index - 1, 0));
            } else if (event.key === 'Enter' && open) {
              const option = options[activeIndex];
              if (option) {
                event.preventDefault();
                choose(option);
              }
            } else if (event.key === 'Escape') {
              setOpen(false);
              setSearch('');
            }
          }}
        />

        {value && !disabled ? (
          <button
            type="button"
            onClick={clear}
            aria-label="Clear selection"
            className="text-ink-faint hover:text-foreground focus-visible:ring-ring shrink-0 rounded-md px-1.5 py-1 focus-visible:ring-2 focus-visible:outline-none"
          >
            <X className="size-3.5" aria-hidden />
          </button>
        ) : null}
      </div>

      {open && anchor
        ? createPortal(
            <ul
              ref={listRef}
              id={listId}
              role="listbox"
              // Fixed, in viewport coordinates, outside every clipping ancestor.
              // ponytail: no viewport flip. Add when a picker lands within ~280px
              // of the fold; that needs a bottom-space check, not a library.
              style={{
                position: 'fixed',
                top: anchor.bottom + 4,
                left: anchor.left,
                width: anchor.width,
              }}
              className="border-border bg-card z-50 max-h-64 overflow-y-auto rounded-lg border p-1 shadow-lg"
            >
              {query.isFetching && options.length === 0 ? (
            <li className="text-ink-faint px-2 py-1.5 text-sm">Searching…</li>
          ) : null}

          {!query.isFetching && options.length === 0 ? (
            <li className="text-ink-faint px-2 py-1.5 text-sm">{emptyHint}</li>
          ) : null}

          {options.map((option, index) => (
            <li key={option.value}>
              <button
                type="button"
                role="option"
                aria-selected={option.value === value}
                // pointerdown, not click: the input's blur fires first and would
                // otherwise tear the list down before the click lands.
                onPointerDown={(event) => {
                  event.preventDefault();
                  choose(option);
                }}
                onMouseEnter={() => setActiveIndex(index)}
                className={cn(
                  'flex w-full flex-col items-start gap-0.5 rounded-md px-2 py-1.5 text-left text-sm',
                  index === activeIndex ? 'bg-accent text-foreground' : 'text-muted-foreground',
                )}
              >
                <span className="font-medium">{option.label}</span>
                {option.hint ? <span className="text-ink-faint text-xs">{option.hint}</span> : null}
              </button>
            </li>
          ))}

          {/* Says so when there is more, rather than silently showing the first
              twenty as if they were all of them. */}
              {total > options.length ? (
                <li className="text-ink-faint border-border mt-1 border-t px-2 py-1.5 text-xs">
                  Showing {options.length} of {total}. Type to narrow it down.
                </li>
              ) : null}
            </ul>,
            document.body,
          )
        : null}
    </div>
  );
}
