'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useMemo } from 'react';

import { useServerTable, type ServerTableState } from '@/components/data-table/use-server-table';
import TreeList, {
  type SortDescriptor,
  type TreeColumn,
  type TreeListProps,
} from '@/components/tree-list/tree-list';
import type { PagedResult } from '@/lib/api/types';
import { useMasterList } from './use-master-list';

/**
 * `TreeList` indexes rows by column name, so it requires `Record<string, unknown>`.
 * The DTOs are interfaces and never satisfy that — see the note in master-columns.
 * The constraint is met with a cast at this one boundary rather than by loosening
 * every row type in the app.
 */
type Indexable<T> = T & Record<string, unknown>;
type TreeListRowProps<T> = TreeListProps<Indexable<T>>;

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
  externalFilterChips,
  onClearExternalFilters,
  onRowClick,
  rowActions,
  editHref,
  canEdit = false,
}: {
  /** Path segment under `/masters`, e.g. `suppliers`. */
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
  externalFilterChips?: React.ReactNode;
  onClearExternalFilters?: () => void;
  onRowClick?: (row: TRow) => void;
  rowActions?: TreeListRowProps<TRow>['rowActions'];
  /**
   * Where a row opens. Supplying this gives every grid the same Edit affordance —
   * a pinned button plus click-anywhere-on-the-row — instead of six grids each
   * inventing one.
   */
  editHref?: (row: TRow) => string;
  /** Whether the caller may edit. The endpoint enforces it too; this only decides what to draw. */
  canEdit?: boolean;
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
  const { data, isFetching } = useMasterList<TRow>(resource, queryString);

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

  const onSearchChange = useCallback(
    (value: string) => apply({ search: value.trim() || null }),
    [apply],
  );

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
      searchPlaceholder={searchPlaceholder}
      ariaLabel={ariaLabel}
      emptyTitle={emptyTitle ?? 'Nothing to show'}
      emptyHint={emptyHint ?? 'No records match the current filters.'}
      emptyAction={emptyAction}
      stretchColumn={stretchColumn}
      exportLabel="Export page"
      exportFileName={exportFileName}
      toolbarExtras={toolbarExtras}
      externalFilterChips={externalFilterChips}
      onClearExternalFilters={onClearExternalFilters}
      // Clicking anywhere on the row opens it, which is what people try first.
      onRowClick={onRowClick ?? (editHref && canEdit ? openRow : undefined)}
      rowActions={
        rowActions ??
        (editHref && canEdit
          ? {
              caption: '',
              width: 72,
              // The pinned button stays alongside the row click, because these
              // grids are wide and the mouse is often parked far from the row start.
              render: (row) => (
                <button
                  type="button"
                  className="border-line bg-surface text-ink-2 hover:border-line-strong hover:text-ink rounded-lg border px-2 py-0.5 text-xs font-medium"
                  onClick={(event) => {
                    // The row's own handler would fire too and race this one.
                    event.stopPropagation();
                    openRow(row);
                  }}
                >
                  Edit
                </button>
              ),
            }
          : undefined)
      }
      // Flat list: nothing to expand, so the state is a constant.
      expandedKeys={NO_EXPANDED}
      onExpandedKeysChange={noop}
      className="min-h-0 flex-1"
      footerBar={<Pager page={data} state={state} apply={apply} isFetching={isFetching} />}
    />
  );
}

function Pager<TRow>({
  page,
  state,
  apply,
  isFetching,
}: {
  page: PagedResult<TRow> | undefined;
  state: ServerTableState;
  apply: (patch: Partial<ServerTableState>) => void;
  isFetching: boolean;
}) {
  return (
    <div className="text-ink-2 flex items-center justify-between text-xs">
      <span className="tabular-nums">
        {page ? (
          <>
            Page {page.page} of {Math.max(page.totalPages, 1)} · {page.totalCount} records
            {/* The grid keeps the previous page on screen while the next one loads,
                so without this the pager looks frozen rather than busy. */}
            {isFetching ? ' · updating…' : ''}
          </>
        ) : (
          'Loading…'
        )}
      </span>

      <div className="flex items-center gap-2">
        <button
          type="button"
          className="border-line bg-surface text-ink-2 hover:border-line-strong hover:text-ink rounded-lg border px-2.5 py-1 font-medium disabled:opacity-40"
          disabled={!page?.hasPreviousPage}
          onClick={() => apply({ page: state.page - 1 })}
        >
          Previous
        </button>
        <button
          type="button"
          className="border-line bg-surface text-ink-2 hover:border-line-strong hover:text-ink rounded-lg border px-2.5 py-1 font-medium disabled:opacity-40"
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
