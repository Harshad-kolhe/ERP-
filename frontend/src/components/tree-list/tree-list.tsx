'use client';

import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type KeyboardEvent,
  type PointerEvent as ReactPointerEvent,
  type ReactNode,
  type UIEvent,
} from 'react';

import {
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Columns3,
  Download,
  Filter,
  Rows3,
  Search,
  X,
} from 'lucide-react';

import Popover from '@/components/tree-list/popover';

export type RowKey = string | number;
export type SortDirection = 'asc' | 'desc';
export type SortDescriptor = { dataField: string; direction: SortDirection };
export type Density = 'compact' | 'cozy' | 'roomy';

export type CellContext = {
  search: string;
  /** Wraps search hits in <mark>. Use for any text you render yourself. */
  highlight: (text: string) => ReactNode;
  /** Direct children of this row currently passing the filters. */
  matchedChildCount: number;
};

export type TreeColumn<T> = {
  dataField: string;
  caption: string;
  width: number;
  minWidth?: number;
  align?: 'left' | 'center' | 'right';
  /** Never rendered and never offered in the column chooser (sort-only helpers). */
  hidden?: boolean;
  /** Off in the column chooser until the user turns it on. */
  defaultVisible?: boolean;
  allowSorting?: boolean;
  allowFiltering?: boolean;
  /**
   * How a server-driven grid compares this column — one of the operators
   * `FilterOperator` declares on the server.
   *
   * Defaults to `contains`, which is right for the free text most of these
   * columns hold and wrong for the rest: `contains` on a boolean asks SQL Server
   * for `LIKE '%true%'` against a bit column, and on a status it would match
   * "Approved" from a search for "rove". Columns that hold a code, a flag or a
   * number set `eq`.
   */
  filterOperator?: 'contains' | 'eq' | 'startswith' | 'gte' | 'lte';
  allowHiding?: boolean;
  allowResizing?: boolean;
  mono?: boolean;
  /** Canonical text for search, the filter row, CSV export and default rendering. */
  calculateCellValue?: (row: T) => string;
  calculateSortValue?: (row: T) => string | number;
  cellRender?: (row: T, ctx: CellContext) => ReactNode;
};

export type RowAppearance = {
  background?: string;
  accent?: string;
  fontWeight?: CSSProperties['fontWeight'];
  muted?: boolean;
};

/**
 * A column pinned to the right edge that holds per-row controls. It is outside
 * the sortable/filterable column set, so it never appears in the column chooser,
 * the filter row or the CSV export.
 */
export type RowActions<T> = {
  caption: string;
  width: number;
  render: (row: T, ctx: { focused: boolean }) => ReactNode;
};

export type TreeListProps<T extends Record<string, unknown>> = {
  dataSource: T[];
  keyExpr: string;
  parentIdExpr: string;
  rootValue: RowKey;
  columns: TreeColumn<T>[];
  rowActions?: RowActions<T>;
  searchPlaceholder?: string;
  ariaLabel?: string;
  emptyTitle?: string;
  emptyHint?: string;
  /** Extra call to action under the empty state, e.g. "Add the first part". */
  emptyAction?: ReactNode;
  /** Grow to fill the flex parent instead of using a fixed `height`. */
  fillHeight?: boolean;
  /** Marks synthetic grouping rows so counts and search ignore them. */
  isGroupRow?: (row: T) => boolean;
  /** Also dim the frozen first cell of a muted row, and its rendered content. */
  mutedIncludesFrozen?: boolean;
  /** Blend row hover with the row's own tint instead of discarding it. */
  hoverBlendsRowBackground?: boolean;
  defaultShowFilterRow?: boolean;
  filterRowLabel?: string;
  exportLabel?: string;
  /** 'all' searches hidden columns too, so narrowing the default set stays lossless. */
  searchScope?: 'visible' | 'all';
  /** Filter pills owned by the page, rendered in TreeList's active-filter bar. */
  externalFilterChips?: ReactNode;
  /**
   * Replaces "Clear all" outright rather than running after it.
   *
   * A controlled grid has to clear in one write. The uncontrolled path below calls
   * three setters in a row, and each one routes to the page's own state; where that
   * state is a URL, all three rebuild it from the same render's snapshot and the
   * last write restores what the first two cleared. So a controlled caller owns the
   * whole action, and the three-setter path is only used when the grid owns its own
   * state and the setters are plain `useState`.
   */
  onClearAll?: () => void;
  /** dataField of the column that absorbs leftover horizontal space. */
  stretchColumn?: string;
  /** Appended to the root section — used with fillHeight to size within a flex page. */
  className?: string;
  /**
   * Applied before any user sorting — keeps NB lines pinned to the bottom.
   * Pass a stable reference so siblings are not re-sorted on every render.
   */
  baseSort?: SortDescriptor[];
  expandedKeys: Set<RowKey>;
  onExpandedKeysChange: (keys: Set<RowKey>) => void;
  selectedKey?: RowKey | null;
  onRowClick?: (row: T) => void;
  height?: number;
  rowAppearance?: (row: T) => RowAppearance;
  /** Sticky summary bar; receives the rows currently passing search + filters. */
  renderFooter?: (rows: T[], meta: { matchedRows: T[]; total: number }) => ReactNode;
  toolbarExtras?: ReactNode;
  exportFileName?: string;

  /**
   * Controlled search term. Supply with `onSearchChange` to route typing into a
   * query string instead of an in-memory scan. Left undefined, the grid owns it.
   */
  searchValue?: string;
  onSearchChange?: (value: string) => void;

  /** Controlled sort. Same contract as {@link searchValue}. */
  sortValue?: SortDescriptor[];
  onSortChange?: (sorts: SortDescriptor[]) => void;

  /**
   * Controlled per-column filters, keyed by `dataField`. Same contract as
   * {@link searchValue}: supply both and the values travel to the server; leave
   * them undefined and the grid filters its own rows in memory.
   */
  filterValues?: Record<string, string>;
  onFilterValuesChange?: (next: Record<string, string>) => void;

  /**
   * The rows arrive already searched, filtered, sorted and paged.
   * <p>
   * This turns off the in-memory pipeline rather than merely hiding it. Without
   * it a server-paged grid re-sorts and re-filters the 25 rows it happens to
   * hold, so the first column header click silently reorders one page against
   * the other fourteen — the failure this flag exists to make impossible.
   * </p>
   */
  serverDriven?: boolean;

  /** Pager and record count, rendered under the grid. */
  footerBar?: ReactNode;

  /**
   * The rows on screen answer the *previous* query — a filter or page change is in
   * flight and the old page is being held to avoid a flash of empty grid.
   *
   * Worth saying out loud: without it the user types a filter, the rows sit there
   * unchanged for a moment, and the grid looks like it ignored them rather than
   * like it is still working.
   */
  isStale?: boolean;

  /**
   * A panel between the controls band and the grid — the page's own filters,
   * typically. A slot rather than a prop bag because what goes here belongs to
   * the screen, not to the grid: TreeList should not learn what a master filter
   * is in order to render one.
   */
  panel?: ReactNode;
};

const NO_SORT: SortDescriptor[] = [];
const HEADER_HEIGHT = 40;
const FILTER_HEIGHT = 36;
const OVERSCAN = 8;
const INDENT = 20;
const ROW_HEIGHT: Record<Density, number> = { compact: 28, cozy: 34, roomy: 42 };
/** Separates the pinned action column from the cells sliding underneath it. */
const ACTION_SHADOW = '-10px 0 12px -10px var(--tl-pin-shadow)';

function justifyFor(align: TreeColumn<never>['align']): CSSProperties['justifyContent'] {
  if (align === 'right') return 'flex-end';
  if (align === 'center') return 'center';
  return 'flex-start';
}

function textAlignFor(align: TreeColumn<never>['align']): CSSProperties['textAlign'] {
  if (align === 'right') return 'right';
  if (align === 'center') return 'center';
  return 'left';
}

/**
 * One colour utility per cell. The frozen column stays full-strength by default —
 * it is the one column guaranteed to be on screen at any horizontal offset —
 * unless the caller opts into dimming it too.
 */
/**
 * The row fills, in the raw tokens rather than the `--color-*` theme names.
 *
 * `@theme inline` does not put its names on `:root`; Tailwind emits one only when
 * it spots that name in the source, which makes a runtime `var(--color-card)`
 * depend on the scanner having noticed a string in a `.tsx` file. `--card` and
 * `--primary` are written into `:root` and `.dark` directly, so they are simply
 * there. `--tl-pin-shadow` above is read the same way for the same reason.
 */
const ROW_BG = 'var(--card)';

/**
 * The selected row's fill: the brand at twice the strength hover uses, so the two
 * states are told apart by depth of tint rather than by hue. Opaque — a selected
 * row's pinned columns have the same covering job as any other row's.
 */
const SELECTED_ROW_BG = 'color-mix(in srgb, var(--primary) 14%, var(--card))';

function cellTone(muted: boolean | undefined, frozen: boolean, mutedIncludesFrozen: boolean): string {
  if (muted && (mutedIncludesFrozen || !frozen)) return 'text-ink-faint';
  return frozen ? 'text-foreground' : 'text-muted-foreground';
}

function compareValues(a: unknown, b: unknown): number {
  const aEmpty = a === null || a === undefined || a === '';
  const bEmpty = b === null || b === undefined || b === '';
  if (aEmpty && bEmpty) return 0;
  if (aEmpty) return 1;
  if (bEmpty) return -1;
  if (typeof a === 'number' && typeof b === 'number') return a - b;
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' });
}

/** Splits text on the search term and wraps the hits. */
function Highlight({ text, term, active }: Readonly<{ text: string; term: string; active: boolean }>) {
  if (!term || !text) return text || null;

  const lower = text.toLowerCase();
  const needle = term.toLowerCase();
  const pieces: ReactNode[] = [];
  let cursor = 0;
  let found = lower.indexOf(needle);
  let index = 0;

  while (found !== -1) {
    if (found > cursor) pieces.push(text.slice(cursor, found));
    pieces.push(
      <mark key={`${found}-${index}`} className="tl-hit" data-active={active || undefined}>
        {text.slice(found, found + needle.length)}
      </mark>,
    );
    index += 1;
    cursor = found + needle.length;
    found = lower.indexOf(needle, cursor);
  }

  if (!pieces.length) return text;
  if (cursor < text.length) pieces.push(text.slice(cursor));
  return <>{pieces}</>;
}

function toCsvCell(value: string): string {
  return /[",\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
}

export default function TreeList<T extends Record<string, unknown>>({
  dataSource,
  keyExpr,
  parentIdExpr,
  rootValue,
  columns,
  rowActions,
  searchPlaceholder = 'Search…',
  ariaLabel = 'Tree list',
  emptyTitle = 'No rows match',
  emptyHint = 'Try a different term or drop one of the column filters.',
  emptyAction,
  fillHeight = false,
  isGroupRow,
  mutedIncludesFrozen = false,
  hoverBlendsRowBackground = false,
  defaultShowFilterRow = true,
  filterRowLabel = 'Filters',
  exportLabel = 'Export',
  searchScope = 'visible',
  externalFilterChips,
  onClearAll,
  stretchColumn,
  className,
  baseSort = NO_SORT,
  expandedKeys,
  onExpandedKeysChange,
  selectedKey = null,
  onRowClick,
  height = 620,
  rowAppearance,
  renderFooter,
  toolbarExtras,
  exportFileName = 'export',
  searchValue,
  onSearchChange,
  sortValue,
  onSortChange,
  filterValues,
  onFilterValuesChange,
  serverDriven = false,
  footerBar,
  panel,
  isStale = false,
}: TreeListProps<T>) {
  const domId = useId().replace(/[^a-zA-Z0-9-]/g, '');
  const scrollRef = useRef<HTMLDivElement>(null);

  const [internalSearchText, setInternalSearchText] = useState('');
  const [internalFilters, setInternalFilters] = useState<Record<string, string>>({});
  const [internalSorts, setInternalSorts] = useState<SortDescriptor[]>([]);

  // Controlled when the page supplies both halves, uncontrolled otherwise —
  // the same arrangement as search and sort above.
  const filters = filterValues ?? internalFilters;

  const setFilters = useCallback(
    (update: (previous: Record<string, string>) => Record<string, string>) => {
      if (onFilterValuesChange) {
        onFilterValuesChange(update(filterValues ?? {}));
        return;
      }

      setInternalFilters(update);
    },
    [filterValues, onFilterValuesChange],
  );

  // Controlled when the page passes a value, uncontrolled otherwise. A
  // server-driven grid routes both of these into the query string, so the search
  // covers every row in the table rather than the page currently in memory.
  const searchText = searchValue ?? internalSearchText;
  const sorts = sortValue ?? internalSorts;

  const setSearchText = useCallback(
    (value: string) => {
      if (onSearchChange) onSearchChange(value);
      else setInternalSearchText(value);
    },
    [onSearchChange],
  );

  const setSorts = useCallback(
    (update: (previous: SortDescriptor[]) => SortDescriptor[]) => {
      if (onSortChange) onSortChange(update(sortValue ?? []));
      else setInternalSorts(update);
    },
    [onSortChange, sortValue],
  );
  const [density, setDensity] = useState<Density>('cozy');
  const [showFilterRow, setShowFilterRow] = useState(defaultShowFilterRow);
  const [widths, setWidths] = useState<Record<string, number>>({});
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(
    () => new Set(columns.filter((c) => !c.hidden && c.defaultVisible === false).map((c) => c.dataField)),
  );
  const [scroll, setScroll] = useState({ top: 0, left: 0 });
  // Measured scrollport. Height feeds the virtual window when fillHeight drops
  // the fixed height; width tells the pinned action column whether it is still
  // covering anything, so its shadow can switch off at the right edge.
  const [viewport, setViewport] = useState({ height, width: 0 });
  const [focusedKey, setFocusedKey] = useState<RowKey | null>(null);
  const [activeMatch, setActiveMatch] = useState(0);
  const [lastSearch, setLastSearch] = useState('');

  const rowHeight = ROW_HEIGHT[density];
  const searchInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const element = scrollRef.current;
    if (!element || typeof ResizeObserver === 'undefined') return;

    const observer = new ResizeObserver(([entry]) => {
      if (!entry) return;
      const { height: h, width: w } = entry.contentRect;
      setViewport((previous) =>
        previous.height === h && previous.width === w ? previous : { height: h, width: w },
      );
    });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  // Virtualisation always needs a number. Without fillHeight this is the prop,
  // so the fixed-height path is bit-for-bit what it was.
  const viewportHeight = fillHeight ? viewport.height : height;

  const columnByField = useMemo(() => new Map(columns.map((c) => [c.dataField, c])), [columns]);
  const chooserColumns = useMemo(() => columns.filter((c) => !c.hidden), [columns]);
  const visibleColumns = useMemo(
    () => chooserColumns.filter((c) => !hiddenColumns.has(c.dataField)),
    [chooserColumns, hiddenColumns],
  );
  const widthOf = useCallback(
    (column: TreeColumn<T>) => widths[column.dataField] ?? column.width,
    [widths],
  );
  const actionsWidth = rowActions?.width ?? 0;
  const totalWidth = useMemo(
    () => visibleColumns.reduce((sum, c) => sum + widthOf(c), actionsWidth),
    [visibleColumns, widthOf, actionsWidth],
  );

  /** `0 0 w` normally; the stretch column absorbs slack so rows never end short. */
  const flexFor = useCallback(
    (column: TreeColumn<T>) =>
      stretchColumn === column.dataField ? `1 1 ${widthOf(column)}px` : `0 0 ${widthOf(column)}px`,
    [stretchColumn, widthOf],
  );

  const cellText = useCallback((column: TreeColumn<T>, row: T): string => {
    if (column.calculateCellValue) return column.calculateCellValue(row);
    const value = row[column.dataField];
    return value === null || value === undefined ? '' : String(value);
  }, []);

  /* ---------------- hierarchy ---------------- */

  const { rowsByKey, childrenByParent } = useMemo(() => {
    const byKey = new Map<RowKey, T>();
    for (const row of dataSource) byKey.set(row[keyExpr] as RowKey, row);

    const byParent = new Map<RowKey, T[]>();
    for (const row of dataSource) {
      const parentId = row[parentIdExpr] as RowKey;
      // Rows pointing at a missing parent surface at the root instead of vanishing.
      const bucket = byKey.has(parentId) ? parentId : rootValue;
      const list = byParent.get(bucket);
      if (list) list.push(row);
      else byParent.set(bucket, [row]);
    }
    return { rowsByKey: byKey, childrenByParent: byParent };
  }, [dataSource, keyExpr, parentIdExpr, rootValue]);

  /* ---------------- sorting, within each sibling group ---------------- */

  const sortedByParent = useMemo(() => {
    // Server-driven: the rows arrived in the order the database produced. Sorting
    // them again here would reorder one page in isolation, which looks like a bug
    // the moment the user pages forward.
    if (serverDriven) return childrenByParent;

    const activeSorts = [...baseSort, ...sorts];
    if (!activeSorts.length) return childrenByParent;

    const sortValue = (descriptor: SortDescriptor, row: T) => {
      const column = columnByField.get(descriptor.dataField);
      return column?.calculateSortValue ? column.calculateSortValue(row) : row[descriptor.dataField];
    };

    const compare = (a: T, b: T) => {
      for (const descriptor of activeSorts) {
        const result = compareValues(sortValue(descriptor, a), sortValue(descriptor, b));
        if (result !== 0) return descriptor.direction === 'asc' ? result : -result;
      }
      return 0;
    };

    const sorted = new Map<RowKey, T[]>();
    for (const [parentId, children] of childrenByParent) sorted.set(parentId, [...children].sort(compare));
    return sorted;
  }, [childrenByParent, columnByField, sorts, baseSort, serverDriven]);

  /* ---------------- search + filter row ---------------- */

  const activeFilters = useMemo(
    () =>
      Object.entries(filters)
        .map(([field, value]) => [field, value.trim().toLowerCase()] as const)
        .filter(([field, value]) => value !== '' && !hiddenColumns.has(field)),
    [filters, hiddenColumns],
  );
  const search = searchText.trim().toLowerCase();
  const isFiltering = search !== '' || activeFilters.length > 0;

  const matchedKeys = useMemo(() => {
    // Server-driven: every row present already matched. Returning null skips the
    // pruning pass but leaves `search` in place, so hits still highlight.
    if (serverDriven || !isFiltering) return null;
    const matched = new Set<RowKey>();

    // Group captions must not match on their own — a group would survive with
    // none of its children. They still reappear through the ancestor pass below.
    const searchColumns = searchScope === 'all' ? chooserColumns : visibleColumns;

    for (const row of dataSource) {
      if (isGroupRow?.(row)) continue;

      let ok = true;
      for (const [field, value] of activeFilters) {
        const column = columnByField.get(field);
        if (!column) continue;
        if (!cellText(column, row).toLowerCase().includes(value)) {
          ok = false;
          break;
        }
      }
      if (ok && search) {
        ok = searchColumns.some((column) => cellText(column, row).toLowerCase().includes(search));
      }
      if (ok) matched.add(row[keyExpr] as RowKey);
    }
    return matched;
  }, [
    isFiltering,
    dataSource,
    activeFilters,
    search,
    columnByField,
    visibleColumns,
    chooserColumns,
    searchScope,
    isGroupRow,
    cellText,
    keyExpr,
    serverDriven,
  ]);

  /** Matches plus their ancestors, mirroring DevExtreme's "withAncestors" filter mode. */
  const visibleKeys = useMemo(() => {
    if (!matchedKeys) return null;
    const withAncestors = new Set(matchedKeys);

    for (const key of matchedKeys) {
      let current = rowsByKey.get(key);
      while (current) {
        const parentId = current[parentIdExpr] as RowKey;
        const parent = rowsByKey.get(parentId);
        if (!parent || withAncestors.has(parentId)) break;
        withAncestors.add(parentId);
        current = parent;
      }
    }
    return withAncestors;
  }, [matchedKeys, rowsByKey, parentIdExpr]);

  /* ---------------- flatten ---------------- */

  type FlatRow = { row: T; key: RowKey; level: number; hasChildren: boolean; expanded: boolean };

  const flatRows = useMemo(() => {
    const out: FlatRow[] = [];

    const walk = (parentKey: RowKey, level: number) => {
      for (const row of sortedByParent.get(parentKey) ?? []) {
        const key = row[keyExpr] as RowKey;
        if (visibleKeys && !visibleKeys.has(key)) continue;

        const kids = sortedByParent.get(key) ?? [];
        const hasChildren = visibleKeys
          ? kids.some((child) => visibleKeys.has(child[keyExpr] as RowKey))
          : kids.length > 0;
        // While searching, branches stay open so every hit stays reachable.
        const expanded = hasChildren && (isFiltering || expandedKeys.has(key));

        out.push({ row, key, level, hasChildren, expanded });
        if (expanded) walk(key, level + 1);
      }
    };

    walk(rootValue, 0);
    return out;
  }, [sortedByParent, rootValue, keyExpr, visibleKeys, isFiltering, expandedKeys]);

  const footerRows = useMemo(() => flatRows.map((entry) => entry.row), [flatRows]);

  // Computed before flattening, so collapsing a group cannot change it — that is
  // what stops a summary reading "0 of 38" with every group closed.
  const matchedRows = useMemo(
    () => (matchedKeys ? dataSource.filter((row) => matchedKeys.has(row[keyExpr] as RowKey)) : dataSource),
    [matchedKeys, dataSource, keyExpr],
  );

  /* ---------------- match navigation ---------------- */

  const matchIndexes = useMemo(() => {
    if (!search || !matchedKeys) return [];
    const out: number[] = [];
    flatRows.forEach((entry, index) => {
      if (isGroupRow?.(entry.row)) return;
      if (matchedKeys.has(entry.key)) out.push(index);
    });
    return out;
  }, [search, matchedKeys, flatRows, isGroupRow]);

  // Reset to the first hit whenever the term changes — adjusted during render
  // rather than in an effect, which avoids a second render pass.
  if (lastSearch !== search) {
    setLastSearch(search);
    setActiveMatch(0);
  }

  const scrollToIndex = useCallback(
    (index: number) => {
      const element = scrollRef.current;
      if (!element) return;
      const headerBlock = HEADER_HEIGHT + (showFilterRow ? FILTER_HEIGHT : 0);
      const rowTop = headerBlock + index * rowHeight;
      const viewTop = element.scrollTop + headerBlock;
      const viewBottom = element.scrollTop + element.clientHeight;

      if (rowTop < viewTop) element.scrollTop = rowTop - headerBlock;
      else if (rowTop + rowHeight > viewBottom) element.scrollTop = rowTop + rowHeight - element.clientHeight;
    },
    [rowHeight, showFilterRow],
  );

  const goToMatch = useCallback(
    (delta: number) => {
      if (!matchIndexes.length) return;
      const next = (activeMatch + delta + matchIndexes.length) % matchIndexes.length;
      const rowIndex = matchIndexes[next];
      if (rowIndex === undefined) return;

      setActiveMatch(next);
      setFocusedKey(flatRows[rowIndex]?.key ?? null);
      scrollToIndex(rowIndex);
    },
    [activeMatch, matchIndexes, flatRows, scrollToIndex],
  );

  const activeMatchIndex = matchIndexes[activeMatch];
  const activeMatchKey =
    activeMatchIndex === undefined ? undefined : flatRows[activeMatchIndex]?.key;

  /* ---------------- virtual window ---------------- */

  const bodyOffset = HEADER_HEIGHT + (showFilterRow ? FILTER_HEIGHT : 0);
  const endIndex = Math.min(
    flatRows.length,
    Math.ceil((scroll.top + viewportHeight - bodyOffset) / rowHeight) + OVERSCAN,
  );
  const startIndex = Math.min(
    endIndex,
    Math.max(0, Math.floor((scroll.top - bodyOffset) / rowHeight) - OVERSCAN),
  );
  const renderedRows = flatRows.slice(startIndex, endIndex);

  const onScroll = useCallback((event: UIEvent<HTMLDivElement>) => {
    const { scrollTop, scrollLeft } = event.currentTarget;
    setScroll((previous) =>
      previous.top === scrollTop && previous.left === scrollLeft
        ? previous
        : { top: scrollTop, left: scrollLeft },
    );
  }, []);

  /* ---------------- interactions ---------------- */

  const toggleRow = useCallback(
    (key: RowKey) => {
      const next = new Set(expandedKeys);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      onExpandedKeysChange(next);
    },
    [expandedKeys, onExpandedKeysChange],
  );

  const setExpanded = useCallback(
    (key: RowKey, expanded: boolean) => {
      const next = new Set(expandedKeys);
      if (expanded) next.add(key);
      else next.delete(key);
      onExpandedKeysChange(next);
    },
    [expandedKeys, onExpandedKeysChange],
  );

  // `setSorts` must be in the deps. Controlled, it closes over `sortValue`, so an
  // empty array here froze this callback on the first render's sorts — which are
  // always none. Every header click then read "nothing is sorted" and answered
  // `asc`, and a column could never reach `desc` on a server-driven grid.
  const toggleSort = useCallback(
    (field: string, additive: boolean) => {
      setSorts((previous) => {
        const existing = previous.find((s) => s.dataField === field);
        const direction: SortDirection | null = !existing ? 'asc' : existing.direction === 'asc' ? 'desc' : null;
        if (!additive) return direction ? [{ dataField: field, direction }] : [];
        const rest = previous.filter((s) => s.dataField !== field);
        return direction ? [...rest, { dataField: field, direction }] : rest;
      });
    },
    [setSorts],
  );

  // Clears every layer the user can see in the active bar, including the page's own
  // filters — otherwise "Clear all" leaves the grid still filtered.
  const clearFilters = useCallback(() => {
    if (onClearAll) {
      onClearAll();
      return;
    }

    setSearchText('');
    setFilters(() => ({}));
  }, [onClearAll, setFilters, setSearchText]);

  // Same stale-closure trap as `toggleSort`: without `setFilters` in the deps this
  // held the first render's filter values, so hiding a column reset every other
  // column's filter back to whatever was applied when the grid first mounted.
  const toggleColumn = useCallback(
    (field: string) => {
      setHiddenColumns((previous) => {
        const next = new Set(previous);
        if (next.has(field)) next.delete(field);
        else next.add(field);
        return next;
      });
      // A hidden column must not keep filtering silently.
      setFilters((previous) => ({ ...previous, [field]: '' }));
    },
    [setFilters],
  );

  const startResize = useCallback(
    (column: TreeColumn<T>, event: ReactPointerEvent<HTMLSpanElement>) => {
      event.preventDefault();
      event.stopPropagation();
      const handle = event.currentTarget;
      const startX = event.clientX;
      const startWidth = widthOf(column);
      const min = column.minWidth ?? 70;
      handle.setPointerCapture(event.pointerId);

      const onMove = (moveEvent: PointerEvent) => {
        const next = Math.max(min, Math.round(startWidth + moveEvent.clientX - startX));
        setWidths((previous) => ({ ...previous, [column.dataField]: next }));
      };
      const onUp = () => {
        handle.removeEventListener('pointermove', onMove);
        handle.removeEventListener('pointerup', onUp);
      };

      handle.addEventListener('pointermove', onMove);
      handle.addEventListener('pointerup', onUp);
    },
    [widthOf],
  );

  const exportCsv = useCallback(() => {
    const header = ['Level', ...visibleColumns.map((column) => column.caption)];
    const lines = [header.map(toCsvCell).join(',')];
    for (const entry of flatRows) {
      const cells = [String(entry.level + 1), ...visibleColumns.map((column) => cellText(column, entry.row))];
      lines.push(cells.map(toCsvCell).join(','));
    }

    const blob = new Blob([`﻿${lines.join('\r\n')}`], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${exportFileName}.csv`;
    anchor.click();
    URL.revokeObjectURL(url);
  }, [visibleColumns, flatRows, cellText, exportFileName]);

  /* ---------------- keyboard navigation ---------------- */

  const focusedIndex = useMemo(
    () => (focusedKey === null ? -1 : flatRows.findIndex((entry) => entry.key === focusedKey)),
    [focusedKey, flatRows],
  );

  const moveFocus = useCallback(
    (index: number) => {
      const clamped = Math.max(0, Math.min(flatRows.length - 1, index));
      const entry = flatRows[clamped];
      if (!entry) return;
      setFocusedKey(entry.key);
      scrollToIndex(clamped);
    },
    [flatRows, scrollToIndex],
  );

  const onGridKeyDown = useCallback(
    (event: KeyboardEvent<HTMLDivElement>) => {
      const current = focusedIndex >= 0 ? focusedIndex : 0;
      const entry = flatRows[current];

      switch (event.key) {
        case 'ArrowDown':
          event.preventDefault();
          moveFocus(focusedIndex < 0 ? 0 : focusedIndex + 1);
          break;
        case 'ArrowUp':
          event.preventDefault();
          moveFocus(focusedIndex < 0 ? 0 : focusedIndex - 1);
          break;
        case 'ArrowRight':
          if (!entry) break;
          event.preventDefault();
          if (entry.hasChildren && !entry.expanded) setExpanded(entry.key, true);
          else if (entry.hasChildren) moveFocus(current + 1);
          break;
        case 'ArrowLeft': {
          if (!entry) break;
          event.preventDefault();
          if (entry.hasChildren && entry.expanded) {
            setExpanded(entry.key, false);
            break;
          }
          for (let i = current - 1; i >= 0; i -= 1) {
            const candidate = flatRows[i];
            if (candidate && candidate.level < entry.level) {
              moveFocus(i);
              break;
            }
          }
          break;
        }
        case 'Home':
          event.preventDefault();
          moveFocus(0);
          break;
        case 'End':
          event.preventDefault();
          moveFocus(flatRows.length - 1);
          break;
        case 'Enter':
        case ' ':
          if (!entry) break;
          event.preventDefault();
          onRowClick?.(entry.row);
          break;
        default:
          break;
      }
    },
    [focusedIndex, flatRows, moveFocus, setExpanded, onRowClick],
  );

  // Ctrl/Cmd+K focuses the search box from anywhere on the page.
  useEffect(() => {
    const onKeyDown = (event: globalThis.KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        searchInputRef.current?.focus();
        searchInputRef.current?.select();
      }
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, []);

  /* ---------------- render ---------------- */

  const frozenColumn = visibleColumns[0];
  const frozenWidth = frozenColumn ? widthOf(frozenColumn) : 0;
  const frozenShadow = scroll.left > 0 ? `10px 0 12px -10px var(--tl-pin-shadow)` : undefined;
  // Mirror of frozenShadow on the right: no shadow once nothing is hidden behind
  // the pinned column, so it stops looking detached when the grid fits.
  const atRightEdge = viewport.width > 0 && scroll.left + viewport.width >= totalWidth - 1;
  const actionShadow = atRightEdge ? undefined : ACTION_SHADOW;

  return (
    <section
      className={`border-border bg-card overflow-hidden rounded-2xl border shadow-sm ${
        fillHeight ? 'flex min-h-0 flex-col' : ''
      } ${className ?? ''}`}
    >
      {/* ---------- toolbar ---------- */}
      <div className="border-border bg-muted flex shrink-0 flex-wrap items-center gap-2 border-b px-3 py-2.5">
        <div className="relative">
          <Search className="text-ink-faint pointer-events-none absolute top-1/2 left-2.5 h-4 w-4 -translate-y-1/2" />
          <input
            ref={searchInputRef}
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                goToMatch(event.shiftKey ? -1 : 1);
              }
              if (event.key === 'Escape') setSearchText('');
            }}
            placeholder={searchPlaceholder}
            aria-label="Search"
            className="border-border bg-card text-foreground placeholder:text-ink-faint focus:border-primary focus:ring-primary/25 h-8 w-64 rounded-lg border pr-16 pl-8 text-sm outline-none focus:ring-2"
          />
          <kbd className="text-ink-faint border-border bg-muted pointer-events-none absolute top-1/2 right-2 -translate-y-1/2 rounded border px-1 py-0.5 text-[10px] font-medium">
            Ctrl K
          </kbd>
        </div>

        {search && (
          <div className="text-muted-foreground flex items-center gap-1 text-xs">
            <span className="tabular-nums">
              {matchIndexes.length ? `${activeMatch + 1} / ${matchIndexes.length}` : 'no matches'}
            </span>
            <button
              type="button"
              aria-label="Previous match"
              onClick={() => goToMatch(-1)}
              disabled={!matchIndexes.length}
              className="border-border hover:bg-accent rounded-md border p-1 disabled:opacity-40"
            >
              <ChevronLeft className="h-3.5 w-3.5 rotate-90" />
            </button>
            <button
              type="button"
              aria-label="Next match"
              onClick={() => goToMatch(1)}
              disabled={!matchIndexes.length}
              className="border-border hover:bg-accent rounded-md border p-1 disabled:opacity-40"
            >
              <ChevronRight className="h-3.5 w-3.5 rotate-90" />
            </button>
          </div>
        )}

        <div className="flex-1" />

        {toolbarExtras}

        {/* Offered whenever the filters can actually do something: in memory for a
            local grid, and against the database for a server-driven one that has
            supplied `filterValues`. A server-driven grid that has not is the one
            case where the control stays hidden — an inert toggle is worse than no
            toggle, because the user types, nothing narrows, and the grid looks
            broken rather than unfinished. */}
        {(!serverDriven || Boolean(onFilterValuesChange)) && (
          <button
            type="button"
            onClick={() => setShowFilterRow((value) => !value)}
            aria-pressed={showFilterRow}
            title={`Toggle the ${filterRowLabel.toLowerCase()} row`}
            className={`border-border hover:border-line-strong focus-visible:ring-primary inline-flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-colors outline-none focus-visible:ring-2 ${
              showFilterRow ? 'bg-accent text-brand-strong border-primary/30' : 'bg-card text-muted-foreground'
            }`}
          >
            <Filter className="h-3.5 w-3.5" />
            {filterRowLabel}
          </button>
        )}

        <Popover
          title="Choose columns"
          label={
            <>
              <Columns3 className="h-3.5 w-3.5" />
              Columns
              <span className="text-ink-faint tabular-nums">
                {visibleColumns.length}/{chooserColumns.length}
              </span>
            </>
          }
          width={280}
        >
          {() => (
            <div>
              <div className="text-ink-faint flex items-center justify-between px-2 py-1.5 text-[11px] font-semibold tracking-wide uppercase">
                Visible columns
                <button
                  type="button"
                  onClick={() => setHiddenColumns(new Set())}
                  className="text-primary hover:text-brand-strong text-[11px] font-medium normal-case"
                >
                  Show all
                </button>
              </div>
              <div className="max-h-72 overflow-auto">
                {chooserColumns.map((column) => {
                  const shown = !hiddenColumns.has(column.dataField);
                  const locked = column.allowHiding === false;
                  return (
                    <button
                      key={column.dataField}
                      type="button"
                      disabled={locked}
                      aria-pressed={shown}
                      onClick={() => toggleColumn(column.dataField)}
                      className="hover:bg-accent focus-visible:ring-primary flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs outline-none focus-visible:ring-2 disabled:opacity-50"
                    >
                      <span
                        className={`flex h-4 w-4 items-center justify-center rounded border ${
                          shown ? 'bg-primary border-primary text-primary-foreground' : 'border-line-strong'
                        }`}
                      >
                        {shown && <Check className="h-3 w-3" strokeWidth={3} />}
                      </span>
                      <span className="text-foreground flex-1 truncate">{column.caption}</span>
                      {locked && <span className="text-ink-faint text-[10px]">pinned</span>}
                    </button>
                  );
                })}
              </div>
            </div>
          )}
        </Popover>

        <Popover
          title="Row density"
          label={
            <>
              <Rows3 className="h-3.5 w-3.5" />
              Density
            </>
          }
          width={170}
        >
          {(close) => (
            <div role="radiogroup" aria-label="Row density">
              {(['compact', 'cozy', 'roomy'] as const).map((option) => (
                <button
                  key={option}
                  type="button"
                  role="radio"
                  aria-checked={density === option}
                  onClick={() => {
                    setDensity(option);
                    close();
                  }}
                  className="hover:bg-accent focus-visible:ring-primary flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs capitalize outline-none focus-visible:ring-2"
                >
                  <span
                    className={`flex h-4 w-4 items-center justify-center rounded-full border ${
                      density === option ? 'bg-primary border-primary text-primary-foreground' : 'border-line-strong'
                    }`}
                  >
                    {density === option && <Check className="h-2.5 w-2.5" strokeWidth={3} />}
                  </span>
                  <span className="text-foreground">{option}</span>
                </button>
              ))}
            </div>
          )}
        </Popover>

        <button
          type="button"
          onClick={exportCsv}
          title="Export the rows currently in view to CSV"
          className="border-border bg-card text-muted-foreground hover:border-line-strong hover:text-foreground inline-flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-colors"
        >
          <Download className="h-3.5 w-3.5" />
          {exportLabel}
        </button>
      </div>

      {/* ---------- active filter chips ---------- */}
      {(isFiltering || externalFilterChips) && (
        <div className="border-border bg-card flex shrink-0 flex-wrap items-center gap-1.5 border-b px-3 py-2">
          <span className="text-ink-faint text-[11px] font-semibold tracking-wide uppercase">Active</span>
          {externalFilterChips}
          {search && (
            <FilterChip label="Search" value={searchText} onClear={() => setSearchText('')} />
          )}
          {activeFilters.map(([field]) => (
            <FilterChip
              key={field}
              label={columnByField.get(field)?.caption ?? field}
              value={filters[field] ?? ''}
              onClear={() => setFilters((previous) => ({ ...previous, [field]: '' }))}
            />
          ))}
          <button
            type="button"
            onClick={clearFilters}
            className="text-ink-faint hover:text-foreground ml-1 text-[11px] font-medium underline underline-offset-2"
          >
            Clear all
          </button>
          <span className="text-ink-faint ml-auto text-xs tabular-nums">
            {isGroupRow ? flatRows.filter((entry) => !isGroupRow(entry.row)).length : flatRows.length} of{' '}
            {isGroupRow ? dataSource.filter((row) => !isGroupRow(row)).length : dataSource.length} rows
          </span>
        </div>
      )}

      {/* The page's own filter panel, between the controls that open it and the
          rows it narrows — which is where someone looks for it after clicking. */}
      {panel}

      {/* ---------- grid ---------- */}
      <div
        ref={scrollRef}
        role="treegrid"
        aria-label={ariaLabel}
        aria-rowcount={flatRows.length + 1 + (showFilterRow ? 1 : 0)}
        aria-colcount={visibleColumns.length + (rowActions ? 1 : 0)}
        tabIndex={0}
        onKeyDown={onGridKeyDown}
        aria-activedescendant={focusedKey !== null ? `${domId}-row-${focusedKey}` : undefined}
        aria-busy={isStale || undefined}
        // The dimming is one CSS rule keyed off this attribute rather than a class
        // threaded down through the row renderer — see globals.css.
        data-stale={isStale || undefined}
        // A column flex container so the empty state can be a sibling of the
        // columns rather than a child of them — see the note where it is rendered.
        className={`focus-visible:ring-primary/40 flex flex-col overflow-auto outline-none focus-visible:ring-2 focus-visible:ring-inset ${
          fillHeight ? 'min-h-0 flex-1' : ''
        }`}
        style={{ height: fillHeight ? undefined : height }}
        onScroll={onScroll}
      >
        <div
          // `shrink-0` because this is now a flex item: without it the rows would
          // be squeezed to fit the viewport instead of scrolling past it.
          className="shrink-0"
          style={
            stretchColumn
              ? { width: `max(${Math.max(totalWidth, 0)}px, 100%)` }
              : { width: Math.max(totalWidth, 0), minWidth: '100%' }
          }
        >
          {/* header */}
          <div role="rowgroup">
          <div
            role="row"
            aria-rowindex={1}
            className="bg-muted sticky top-0 z-20 flex"
            style={{ height: HEADER_HEIGHT }}
          >
            {visibleColumns.map((column, index) => {
              const sortIndex = sorts.findIndex((s) => s.dataField === column.dataField);
              const sort = sortIndex >= 0 ? sorts[sortIndex] : null;
              const sortable = column.allowSorting !== false;
              const frozen = index === 0;
              let ariaSort: 'ascending' | 'descending' | undefined;
              if (sort) ariaSort = sort.direction === 'asc' ? 'ascending' : 'descending';

              return (
                <div
                  key={column.dataField}
                  role="columnheader"
                  aria-colindex={index + 1}
                  tabIndex={sortable ? 0 : -1}
                  aria-sort={ariaSort}
                  className={`border-border group/head focus-visible:ring-primary relative flex items-center gap-1 border-r border-b px-2.5 text-[11.5px] font-semibold tracking-wide outline-none select-none focus-visible:ring-2 focus-visible:ring-inset ${
                    sortable ? 'hover:bg-accent cursor-pointer' : ''
                  } ${frozen ? 'bg-muted sticky left-0 z-30' : ''} ${sort ? 'text-brand-strong' : 'text-muted-foreground'}`}
                  style={{
                    flex: flexFor(column),
                    width: widthOf(column),
                    justifyContent: justifyFor(column.align),
                    boxShadow: frozen ? frozenShadow : undefined,
                  }}
                  onClick={
                    sortable
                      ? (event) => toggleSort(column.dataField, event.shiftKey || event.ctrlKey || event.metaKey)
                      : undefined
                  }
                  onKeyDown={
                    sortable
                      ? (event) => {
                          if (event.key !== 'Enter' && event.key !== ' ') return;
                          event.preventDefault();
                          toggleSort(column.dataField, event.shiftKey);
                        }
                      : undefined
                  }
                  title={sortable ? `${column.caption} — click to sort, Shift+click to add a level` : column.caption}
                >
                  <span className="truncate uppercase">{column.caption}</span>
                  {sort && (
                    <span className="text-primary flex shrink-0 items-center text-[10px]">
                      <ChevronDown
                        className={`h-3 w-3 transition-transform ${sort.direction === 'asc' ? 'rotate-180' : ''}`}
                      />
                      {sorts.length > 1 && <span className="tabular-nums">{sortIndex + 1}</span>}
                    </span>
                  )}
                  {column.allowResizing !== false && (
                    <span
                      role="presentation"
                      onPointerDown={(event) => startResize(column, event)}
                      onClick={(event) => event.stopPropagation()}
                      className="hover:bg-primary/60 absolute top-0 right-0 z-10 h-full w-1.5 cursor-col-resize"
                    />
                  )}
                </div>
              );
            })}

            {rowActions && (
              <div
                role="columnheader"
                aria-colindex={visibleColumns.length + 1}
                className="border-border bg-muted text-muted-foreground sticky right-0 z-30 flex items-center justify-center border-b border-l px-2.5 text-[11.5px] font-semibold tracking-wide uppercase"
                style={{ flex: `0 0 ${actionsWidth}px`, width: actionsWidth, boxShadow: actionShadow }}
              >
                {rowActions.caption}
              </div>
            )}
          </div>

          {/* filter row */}
          {showFilterRow && (
            <div
              role="row"
              aria-rowindex={2}
              className="bg-card sticky z-20 flex"
              style={{ top: HEADER_HEIGHT, height: FILTER_HEIGHT }}
            >
              {visibleColumns.map((column, index) => {
                const frozen = index === 0;
                return (
                  <div
                    key={column.dataField}
                    role="gridcell"
                    aria-colindex={index + 1}
                    className={`border-border flex items-center border-r border-b px-1.5 ${
                      frozen ? 'bg-card sticky left-0 z-30' : ''
                    }`}
                    style={{
                      flex: flexFor(column),
                      width: widthOf(column),
                      boxShadow: frozen ? frozenShadow : undefined,
                    }}
                  >
                    {column.allowFiltering === false ? null : (
                      <input
                        value={filters[column.dataField] ?? ''}
                        onChange={(event) =>
                          setFilters((previous) => ({ ...previous, [column.dataField]: event.target.value }))
                        }
                        placeholder="Filter…"
                        aria-label={`Filter ${column.caption}`}
                        className="text-foreground placeholder:text-ink-faint/70 focus:border-primary focus:bg-card focus:ring-primary/40 hover:border-border w-full rounded-md border border-transparent bg-transparent px-1.5 py-1 text-[12px] outline-none focus:ring-2 focus:ring-inset"
                        style={{ textAlign: textAlignFor(column.align) }}
                      />
                    )}
                  </div>
                );
              })}

              {rowActions && (
                <div
                  role="gridcell"
                  aria-colindex={visibleColumns.length + 1}
                  className="border-border bg-card sticky right-0 z-30 border-b border-l"
                  style={{ flex: `0 0 ${actionsWidth}px`, width: actionsWidth, boxShadow: actionShadow }}
                />
              )}
            </div>
          )}
          </div>

          {/* body */}
          {flatRows.length > 0 && (
            <div role="rowgroup">
              <div role="presentation" aria-hidden="true" style={{ height: startIndex * rowHeight }} />
              {renderedRows.map(({ row, key, level, hasChildren, expanded }, renderIndex) => {
                const appearance = rowAppearance?.(row) ?? {};
                const selected = selectedKey === key;
                const focused = focusedKey === key;
                const isActiveMatch = activeMatchKey === key;
                const kids = sortedByParent.get(key) ?? [];
                const context: CellContext = {
                  search,
                  highlight: (text: string) => (
                    <Highlight text={text} term={search} active={isActiveMatch} />
                  ),
                  matchedChildCount: visibleKeys
                    ? kids.filter((child) => visibleKeys.has(child[keyExpr] as RowKey)).length
                    : kids.length,
                };
                /*
                 * The grid's own frame is `bg-card`, so an untinted row matching it
                 * is what "no background" is supposed to look like.
                 *
                 * This used to say `--color-surface`, and `--color-brand-soft`
                 * below, and neither has ever existed in the theme. An undefined
                 * `var()` makes the whole `background-color` declaration invalid,
                 * so every row resolved to transparent — indistinguishable from
                 * correct until a pinned cell has to cover something, and then the
                 * frozen first column and the actions column let the scrolled rows
                 * show straight through.
                 */
                const rowBackground = appearance.background ?? ROW_BG;

                return (
                  <div
                    key={key}
                    id={`${domId}-row-${key}`}
                    role="row"
                    aria-level={level + 1}
                    aria-rowindex={startIndex + renderIndex + 2 + (showFilterRow ? 1 : 0)}
                    aria-selected={selected}
                    aria-expanded={hasChildren ? expanded : undefined}
                    onClick={() => {
                      setFocusedKey(key);
                      onRowClick?.(row);
                    }}
                    className="group border-line-soft relative flex border-b bg-[var(--row-bg)] hover:bg-[var(--row-hover)]"
                    style={
                      {
                        height: rowHeight,
                        fontWeight: appearance.fontWeight,
                        cursor: onRowClick ? 'pointer' : undefined,
                        '--row-bg': selected ? SELECTED_ROW_BG : rowBackground,
                        // Blending against the row's own tint keeps a tinted group
                        // row distinguishable from a hovered data row.
                        '--row-hover': (() => {
                          if (selected) return SELECTED_ROW_BG;
                          const base = hoverBlendsRowBackground ? rowBackground : ROW_BG;
                          return `color-mix(in srgb, var(--primary) 7%, ${base})`;
                        })(),
                      } as CSSProperties
                    }
                  >
                    {visibleColumns.map((column, columnIndex) => {
                      const frozen = columnIndex === 0;
                      const text = cellText(column, row);
                      const content = column.cellRender
                        ? column.cellRender(row, context)
                        : context.highlight(text);

                      return (
                        <div
                          key={column.dataField}
                          role="gridcell"
                          aria-colindex={columnIndex + 1}
                          // Size and colour are single, mutually exclusive choices:
                          // emitting two of either left the winner to stylesheet order.
                          className={`border-line-soft flex items-center overflow-hidden border-r px-2.5 ${
                            column.mono ? 'font-mono text-[11.5px]' : 'text-[12.5px]'
                          } ${cellTone(appearance.muted, frozen, mutedIncludesFrozen)} ${
                            frozen ? 'sticky left-0 z-10 bg-[var(--row-bg)] group-hover:bg-[var(--row-hover)]' : ''
                          }`}
                          style={{
                            flex: flexFor(column),
                            width: widthOf(column),
                            justifyContent: justifyFor(column.align),
                            paddingLeft: frozen ? 10 + level * INDENT : undefined,
                            boxShadow: frozen ? frozenShadow : undefined,
                          }}
                          title={!frozen && text ? text : undefined}
                        >
                          {frozen && (
                            <>
                              {/* indent rails */}
                              {Array.from({ length: level }, (_, depth) => (
                                <span
                                  key={depth}
                                  aria-hidden="true"
                                  className="bg-border pointer-events-none absolute top-0 h-full w-px"
                                  style={{ left: 17 + depth * INDENT }}
                                />
                              ))}
                              {/* line-type accent */}
                              {appearance.accent && (
                                <span
                                  aria-hidden="true"
                                  className="pointer-events-none absolute top-0 left-0 h-full w-[3px]"
                                  style={{ background: appearance.accent }}
                                />
                              )}
                              <button
                                type="button"
                                tabIndex={-1}
                                aria-label={expanded ? 'Collapse' : 'Expand'}
                                onClick={(event) => {
                                  event.stopPropagation();
                                  if (hasChildren) toggleRow(key);
                                }}
                                className={`text-ink-faint hover:text-foreground hover:bg-accent mr-1 flex h-5 w-5 shrink-0 items-center justify-center rounded-md transition-colors ${
                                  hasChildren ? '' : 'pointer-events-none opacity-0'
                                }`}
                              >
                                <ChevronRight
                                  className={`h-3.5 w-3.5 transition-transform duration-150 ${
                                    expanded ? 'rotate-90' : ''
                                  }`}
                                />
                              </button>
                            </>
                          )}
                          {/* Opacity belongs on the content, never on the sticky cell:
                              a stacking context there lets scrolling cells show through. */}
                          <span
                            className={`flex min-w-0 items-center gap-1.5 truncate ${
                              mutedIncludesFrozen && appearance.muted ? 'opacity-60' : ''
                            }`}
                          >
                            {content}
                          </span>
                        </div>
                      );
                    })}

                    {rowActions && (
                      <div
                        role="gridcell"
                        aria-colindex={visibleColumns.length + 1}
                        onClick={(event) => event.stopPropagation()}
                        className="border-line-soft sticky right-0 z-10 flex items-center justify-center gap-1 border-l bg-[var(--row-bg)] px-1.5 group-hover:bg-[var(--row-hover)]"
                        style={{
                          flex: `0 0 ${actionsWidth}px`,
                          width: actionsWidth,
                          boxShadow: actionShadow,
                        }}
                      >
                        {rowActions.render(row, { focused })}
                      </div>
                    )}

                    {focused && (
                      <span
                        aria-hidden="true"
                        className="ring-primary/70 pointer-events-none absolute inset-0 z-[15] rounded-sm ring-2 ring-inset"
                      />
                    )}
                  </div>
                );
              })}
              <div
                role="presentation"
                aria-hidden="true"
                style={{ height: Math.max(0, (flatRows.length - endIndex) * rowHeight) }}
              />
            </div>
          )}
        </div>

        {/*
          Outside the column container on purpose. In there its width resolved
          against the full width of every column, so it centred itself across the
          scroll extent and had to be clamped to stay on screen at all — which put
          it a fixed distance from the left edge rather than in the middle of
          anything. Out here it is a flex item of the scroll port, so `stretch`
          gives it exactly the width the operator can see and `flex-1` gives it the
          height left under the header. Still sticky, so scrolling sideways past
          the header does not carry it off.
        */}
        {flatRows.length === 0 && (
          <EmptyState
            onClear={clearFilters}
            // A master with no records at all is not a filtered-to-nothing grid.
            // `isFiltering` covers this component's own surfaces; the chips cover
            // the page's, which on a server-driven grid is where the filter that
            // emptied it usually lives.
            isFiltering={isFiltering || Boolean(externalFilterChips)}
            title={emptyTitle}
            hint={emptyHint}
            action={emptyAction}
          />
        )}
      </div>

      {/* ---------- summary bar ---------- */}
      {renderFooter && (
        <div
          className="border-border bg-muted shrink-0 border-t px-3 py-2"
          style={{ paddingLeft: Math.min(frozenWidth, 24) + 8 }}
        >
          {renderFooter(footerRows, { matchedRows, total: dataSource.length })}
        </div>
      )}

      {/* ---------- pager ---------- */}
      {footerBar && (
        <div className="border-border bg-muted shrink-0 border-t px-3 py-2">{footerBar}</div>
      )}
    </section>
  );
}

export function FilterChip({
  label,
  value,
  onClear,
}: Readonly<{ label: string; value: string; onClear: () => void }>) {
  return (
    <span className="border-primary/30 bg-accent text-brand-strong inline-flex items-center gap-1 rounded-full border py-0.5 pr-1 pl-2 text-[11px] font-medium">
      <span className="text-ink-faint">{label}:</span>
      <span className="max-w-32 truncate">{value}</span>
      <button
        type="button"
        aria-label={`Clear ${label} filter`}
        onClick={onClear}
        className="hover:bg-primary/20 rounded-full p-0.5"
      >
        <X className="h-2.5 w-2.5" strokeWidth={2.6} />
      </button>
    </span>
  );
}

function EmptyState({
  onClear,
  isFiltering,
  title,
  hint,
  action,
}: Readonly<{
  onClear: () => void;
  isFiltering: boolean;
  title: string;
  hint: string;
  action?: ReactNode;
}>) {
  return (
    <div className="sticky left-0 flex min-h-56 flex-1 flex-col items-center justify-center gap-2 px-4 text-center">
      <div className="bg-accent text-ink-faint flex h-11 w-11 items-center justify-center rounded-full">
        <Search className="h-5 w-5" />
      </div>
      <p className="text-foreground text-sm font-medium">{title}</p>
      <p className="text-ink-faint text-xs">{hint}</p>
      <div className="mt-1 flex flex-wrap items-center justify-center gap-2">
        {/* Only when there is something to clear. Offered unconditionally it was a
            button that did nothing on an empty master, which teaches people that
            the control does not work rather than that the master is empty. */}
        {isFiltering && (
          <button
            type="button"
            onClick={onClear}
            className="border-border bg-card text-muted-foreground hover:border-line-strong hover:text-foreground rounded-lg border px-3 py-1.5 text-xs font-medium"
          >
            Clear all filters
          </button>
        )}
        {action}
      </div>
    </div>
  );
}
