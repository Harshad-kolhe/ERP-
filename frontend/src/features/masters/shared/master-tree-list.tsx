'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useMemo, useState } from 'react';

import { useServerTable, type ServerTableState } from '@/components/data-table/use-server-table';
import TreeList, {
  FilterChip,
  type SortDescriptor,
  type TreeColumn,
} from '@/components/tree-list/tree-list';
import type { PagedResult } from '@/lib/api/types';
import { countActive, replaceOwnedTerms, valuesFor, type FilterTerm } from './filter-terms';
import {
  MasterFilters,
  MasterFiltersTrigger,
  displayValue,
  filterLookups,
  type MasterFilterField,
} from './master-filters';
import { useLookups } from './use-lookups';
import { useMasterList } from './use-master-list';

/**
 * `TreeList` indexes rows by column name, so it requires `Record<string, unknown>`.
 * The DTOs are interfaces and never satisfy that — see the note in master-columns.
 * The constraint is met with a cast at this one boundary rather than by loosening
 * every row type in the app.
 */
type Indexable<T> = T & Record<string, unknown>;

/**
 * Every master list screen.
 *
 * One component, not one per master: the four grids differ in their columns and
 * nothing else, so anything else that varied between them would be an accident
 * rather than a decision. A new master is a `columns` array and a route.
 *
 * It is the bridge between two halves that disagree by design. `TreeList` was
 * built against an in-memory array and would happily search, sort and filter it;
 * this API returns one page and expects the database to have done that work. The
 * bridge is `serverDriven`, which switches off the in-memory pipeline, plus the
 * controlled search and sort props that send the user's intent to the query
 * string instead. The visible result is the prototype's grid; the mechanism
 * underneath is the paged one.
 */
export function MasterTreeList<TRow>({
  resource,
  columns,
  keyField,
  searchPlaceholder,
  ariaLabel,
  emptyTitle,
  emptyHint,
  emptyAction,
  stretchColumn,
  exportFileName,
  toolbarExtras,
  onRowClick,
  editHref,
  canEdit = false,
  filters,
  filtersNoun,
  basePath = '/masters',
}: {
  /** Path segment under `basePath`, e.g. `suppliers`. */
  resource: string;
  columns: TreeColumn<TRow>[];
  /** Field holding the row's unique id. */
  keyField: string;
  searchPlaceholder: string;
  ariaLabel: string;
  emptyTitle?: string;
  emptyHint?: string;
  emptyAction?: React.ReactNode;
  stretchColumn?: string;
  exportFileName: string;
  toolbarExtras?: React.ReactNode;
  onRowClick?: (row: TRow) => void;
  /**
   * Where a row opens. Supplying this gives every grid the same Edit affordance —
   * a pinned button plus click-anywhere-on-the-row — instead of six grids each
   * inventing one.
   */
  editHref?: (row: TRow) => string;
  /** Whether the caller may edit. The endpoint enforces it too; this only decides what to draw. */
  canEdit?: boolean;
  /**
   * The fields this master's filters panel offers. Omit it and no panel is drawn,
   * so a master that has nothing worth a dedicated search does not grow an empty
   * one. Every field must exist on the endpoint's `QueryMap`.
   */
  filters?: readonly MasterFilterField[];
  /** Names the panel: "Part" gives "Part filters". Defaults to the aria label. */
  filtersNoun?: string;
  /**
   * Endpoint prefix. Defaults to `/masters`; the identity screens live under
   * `/admin`. It also separates the two in the query cache, which matters because
   * `/masters/roles` and `/admin/roles` are different tables with the same name.
   */
  basePath?: string;
}) {
  const router = useRouter();
  const { state, apply, queryString } = useServerTable();

  const openRow = useCallback(
    (row: TRow) => {
      if (editHref && canEdit) {
        router.push(editHref(row));
      }
    },
    [editHref, canEdit, router],
  );
  const { data, isPlaceholderData } = useMasterList<TRow>(resource, queryString, basePath);

  const rows = useMemo(() => data?.items ?? [], [data]);

  /**
   * The grid is flat, so every row is a child of one synthetic root. TreeList is
   * a tree component and needs a parent field; giving every row the same parent
   * costs one property and avoids maintaining a second, flat renderer.
   *
   * `__srNo` is the row's position in the whole result, not on the page, so row 1
   * of page 3 reads 51. Computed here because this is the only place that knows
   * both the page and the page size — see `serialNumberColumn`.
   */
  const dataSource = useMemo(
    () =>
      rows.map((row, index) => ({
        ...row,
        __parent: ROOT,
        __srNo: (state.page - 1) * state.pageSize + index + 1,
      })) as Indexable<TRow>[],
    [rows, state.page, state.pageSize],
  );

  // `sort=field:asc` on the wire, `[{dataField, direction}]` in the grid.
  const sortValue = useMemo<SortDescriptor[]>(() => parseSort(state.sort), [state.sort]);

  const onSortChange = useCallback(
    (next: SortDescriptor[]) =>
      apply({ sort: next.length ? next.map((s) => `${s.dataField}:${s.direction}`).join(',') : null }),
    [apply],
  );

  // `replace`: the search box writes on every keystroke, and pushing would put one
  // history entry per character typed.
  const onSearchChange = useCallback(
    (value: string) => apply({ search: value.trim() || null }, 'replace'),
    [apply],
  );

  /**
   * The per-column filter row, wired to the database rather than to the page in
   * the browser.
   *
   * The grid speaks `{ field: text }`; the API speaks
   * `field:op:value;field:op:value`. Translating here rather than in TreeList
   * keeps the grid ignorant of the query contract — it is also used for local,
   * in-memory trees that have no server behind them at all.
   *
   * A field the server does not declare on its `QueryMap` is rejected with 400
   * rather than ignored, which is deliberate: a filter that silently did nothing
   * would show unfiltered data to someone who believes it is filtered. Columns
   * that have no server field say so with `allowFiltering: false`.
   */
  const panelFields = useMemo(() => filters ?? [], [filters]);

  // The two surfaces divide the query string between them by field. The panel is
  // declared explicitly; the column row owns whatever is left, so neither can
  // erase the other's work when it rewrites its own.
  const panelOwned = useMemo(() => panelFields.map((field) => field.field), [panelFields]);

  const columnOwned = useMemo(
    () =>
      columns
        .map((column) => column.dataField)
        .filter((field) => !panelOwned.includes(field)),
    [columns, panelOwned],
  );

  const operatorByField = useMemo(() => {
    const map = new Map<string, string>();
    for (const column of columns) map.set(column.dataField, column.filterOperator ?? 'contains');
    for (const field of panelFields) map.set(field.field, field.operator ?? 'contains');
    return map;
  }, [columns, panelFields]);

  const toTerms = useCallback(
    (values: Record<string, string>): FilterTerm[] =>
      Object.entries(values)
        .filter(([, value]) => value.trim() !== '')
        .map(([field, value]) => ({
          field,
          operator: (operatorByField.get(field) ?? 'contains') as FilterTerm['operator'],
          value,
        })),
    [operatorByField],
  );

  const filterValues = useMemo(() => valuesFor(state.filter, columnOwned), [state.filter, columnOwned]);

  // `replace`, for the same reason as the search box: this row writes per keystroke.
  const onFilterValuesChange = useCallback(
    (next: Record<string, string>) =>
      apply({ filter: replaceOwnedTerms(state.filter, columnOwned, toTerms(next)) }, 'replace'),
    [apply, state.filter, columnOwned, toTerms],
  );

  // ---- the page's own filters panel

  const panelValues = useMemo(() => valuesFor(state.filter, panelOwned), [state.filter, panelOwned]);
  const panelCount = countActive(panelValues);
  const [panelOpen, setPanelOpen] = useState(false);
  const { lookups } = useLookups(useMemo(() => filterLookups(panelFields), [panelFields]));

  const applyPanel = useCallback(
    (next: Record<string, string>) =>
      apply({ filter: replaceOwnedTerms(state.filter, panelOwned, toTerms(next)) }),
    [apply, state.filter, panelOwned, toTerms],
  );

  // Clears this panel's fields only. The column row's terms are somebody else's,
  // and a Reset that silently emptied them too would be a trap.
  const resetPanel = useCallback(
    () => apply({ filter: replaceOwnedTerms(state.filter, panelOwned, []) }),
    [apply, state.filter, panelOwned],
  );

  /**
   * The panel's applied filters, as chips in the grid's active bar.
   *
   * Without these the panel was the one filter surface with nothing to show for
   * itself: the grid said "Active" and listed only the column row's terms, so a
   * filter applied from the panel narrowed the rows invisibly. Worst after a
   * reload, where the panel starts closed and the only clue was a count on a
   * button.
   */
  const panelChips = useMemo(
    () =>
      panelFields
        .filter((field) => (panelValues[field.field] ?? '').trim() !== '')
        .map((field) => (
          <FilterChip
            key={field.field}
            label={field.label}
            value={displayValue(field, panelValues[field.field] ?? '', lookups)}
            onClear={() => applyPanel({ ...panelValues, [field.field]: '' })}
          />
        )),
    [panelFields, panelValues, lookups, applyPanel],
  );

  /**
   * One write that clears everything the user can see.
   *
   * It has to be one. Clearing used to be three sequential calls — search, then
   * the column row, then the page's own filters — and each rebuilt the URL from
   * the render it was queued in, so the last one restored what the first two had
   * removed and "Clear all" left the grid filtered. Setting both keys to null in a
   * single patch also reaches the status chips above the grid, whose term lives in
   * the same string but belongs to no filter surface at all.
   */
  const clearAll = useCallback(() => apply({ search: null, filter: null }), [apply]);

  return (
    <TreeList<Indexable<TRow>>
      dataSource={dataSource}
      keyExpr={keyField}
      parentIdExpr="__parent"
      rootValue={ROOT}
      columns={columns as TreeColumn<Indexable<TRow>>[]}
      fillHeight
      serverDriven
      searchValue={state.search ?? ''}
      onSearchChange={onSearchChange}
      sortValue={sortValue}
      onSortChange={onSortChange}
      filterValues={filterValues}
      onFilterValuesChange={onFilterValuesChange}
      toolbarExtras={
        <>
          {panelFields.length > 0 && (
            <MasterFiltersTrigger
              open={panelOpen}
              appliedCount={panelCount}
              onToggle={() => setPanelOpen((open) => !open)}
            />
          )}
          {toolbarExtras}
        </>
      }
      panel={
        panelFields.length > 0 && panelOpen ? (
          <MasterFilters
            noun={filtersNoun ?? ariaLabel}
            fields={panelFields}
            values={panelValues}
            lookups={lookups}
            onApply={applyPanel}
            onReset={resetPanel}
            onClose={() => setPanelOpen(false)}
          />
        ) : null
      }
      searchPlaceholder={searchPlaceholder}
      ariaLabel={ariaLabel}
      // Named as the Part Master prototype names it. "Filters" alone reads as the
      // page's filters; these are the per-column boxes in the header.
      filterRowLabel="Column filters"
      emptyTitle={emptyTitle ?? 'Nothing to show'}
      emptyHint={emptyHint ?? 'No records match the current filters.'}
      emptyAction={emptyAction}
      stretchColumn={stretchColumn}
      exportLabel="Export page"
      exportFileName={exportFileName}
      externalFilterChips={panelChips.length > 0 ? panelChips : undefined}
      onClearAll={clearAll}
      isStale={isPlaceholderData}
      /*
       * Navigating is the Edit button's job and nothing else's.
       *
       * A whole-row click used to open the record too. On a grid this wide that
       * is a trap rather than a shortcut: selecting a cell to read it, dragging
       * to compare two columns, or clicking to dismiss a popover all land on a
       * row, and any of them would throw away the filters and scroll position
       * the user had built up. One deliberate target, always in the same place.
       *
       * `onRowClick` is still honoured when a screen passes its own — the grid
       * keeps the capability; the masters simply decline to navigate with it.
       */
      onRowClick={onRowClick}
      rowActions={
        editHref && canEdit
          ? {
              // Named rather than blank: an empty caption renders a column header
              // with no accessible name, which a screen reader reads as nothing at
              // all where every other column announces itself.
              caption: 'Actions',
              width: 88,
              render: (row) => (
                <button
                  type="button"
                  className="border-border bg-card text-muted-foreground hover:border-line-strong hover:text-foreground focus-visible:ring-ring rounded-lg border px-2 py-0.5 text-xs font-medium focus-visible:ring-2 focus-visible:outline-none"
                  onClick={() => openRow(row)}
                >
                  Edit
                </button>
              ),
            }
          : undefined
      }
      // Flat list: nothing to expand, so the state is a constant.
      expandedKeys={NO_EXPANDED}
      onExpandedKeysChange={noop}
      className="min-h-0 flex-1"
      footerBar={<Pager page={data} state={state} apply={apply} isStale={isPlaceholderData} />}
    />
  );
}

function Pager<TRow>({
  page,
  state,
  apply,
  isStale,
}: {
  page: PagedResult<TRow> | undefined;
  state: ServerTableState;
  apply: (patch: Partial<ServerTableState>) => void;
  /** The rows on screen belong to the previous query, not the current one. */
  isStale: boolean;
}) {
  return (
    <div className="text-muted-foreground flex items-center justify-between text-xs">
      <span className="tabular-nums">
        {page ? (
          <>
            Page {page.page} of {Math.max(page.totalPages ?? 0, 1)} · {page.totalCount} records
            {/* The grid keeps the previous page on screen while the next one loads,
                so without this the pager looks frozen rather than busy. Keyed to
                `isPlaceholderData` and not `isFetching`: a background refetch of
                the query already on screen is not stale, and saying so on every
                revalidation trains people to ignore the word. */}
            {isStale ? ' · updating…' : ''}
          </>
        ) : (
          'Loading…'
        )}
      </span>

      <div className="flex items-center gap-2">
        <button
          type="button"
          className="border-border bg-card text-muted-foreground hover:border-line-strong hover:text-foreground rounded-lg border px-2.5 py-1 font-medium disabled:opacity-40"
          disabled={!page?.hasPreviousPage}
          onClick={() => apply({ page: state.page - 1 })}
        >
          Previous
        </button>
        <button
          type="button"
          className="border-border bg-card text-muted-foreground hover:border-line-strong hover:text-foreground rounded-lg border px-2.5 py-1 font-medium disabled:opacity-40"
          disabled={!page?.hasNextPage}
          onClick={() => apply({ page: state.page + 1 })}
        >
          Next
        </button>
      </div>
    </div>
  );
}

/** Parses the wire format `field:asc,other:desc` into TreeList's descriptors. */
function parseSort(sort: string | null): SortDescriptor[] {
  if (!sort) return [];

  return sort
    .split(',')
    .map((term) => term.split(':'))
    .filter(([field, direction]) => field && (direction === 'asc' || direction === 'desc'))
    .map(([field, direction]) => ({
      dataField: field as string,
      direction: direction as 'asc' | 'desc',
    }));
}

const ROOT = '__root';
const NO_EXPANDED: Set<string | number> = new Set();
const noop = () => {};
