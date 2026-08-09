'use client';

import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useCallback, useMemo } from 'react';

/** Mirrors `PageRequest.MaxPageSize`. The server clamps regardless of what we send. */
export const MAX_PAGE_SIZE = 200;
export const DEFAULT_PAGE_SIZE = 25;

export interface ServerTableState {
  page: number;
  pageSize: number;
  sort: string | null;
  search: string | null;
  filter: string | null;
}

/**
 * Holds grid state in the URL and turns it into the API's query string.
 *
 * This hook is the only sanctioned way to build a list screen, and it is why
 * "download the whole table and page in the browser" is not expressible here.
 * There is no client-side pagination path: the state it produces goes to the
 * server, and the server returns one page. In the system this replaces, roughly
 * 149 of 180 grids fetched every matching row — retrofitting them would have been
 * a 149-screen project.
 *
 * Keeping state in the URL rather than component state also means a filtered
 * grid is a link someone can paste into a message, and the back button works.
 */
export function useServerTable(initial?: Partial<ServerTableState>) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const state = useMemo<ServerTableState>(() => {
    const page = Number.parseInt(searchParams.get('page') ?? '', 10);
    const pageSize = Number.parseInt(searchParams.get('pageSize') ?? '', 10);

    return {
      page: Number.isFinite(page) && page > 0 ? page : (initial?.page ?? 1),
      pageSize:
        Number.isFinite(pageSize) && pageSize > 0
          ? Math.min(pageSize, MAX_PAGE_SIZE)
          : (initial?.pageSize ?? DEFAULT_PAGE_SIZE),
      sort: searchParams.get('sort') ?? initial?.sort ?? null,
      search: searchParams.get('search') ?? initial?.search ?? null,
      filter: searchParams.get('filter') ?? initial?.filter ?? null,
    };
  }, [searchParams, initial]);

  const apply = useCallback(
    (patch: Partial<ServerTableState>) => {
      const next = new URLSearchParams(searchParams.toString());
      const merged = { ...state, ...patch };

      // Changing what is being looked at must reset the position, or the user
      // lands on page 7 of a 2-page result and sees nothing.
      if (patch.search !== undefined || patch.filter !== undefined || patch.pageSize !== undefined) {
        merged.page = 1;
      }

      const entries: [string, string | number | null][] = [
        ['page', merged.page === 1 ? null : merged.page],
        ['pageSize', merged.pageSize === DEFAULT_PAGE_SIZE ? null : merged.pageSize],
        ['sort', merged.sort],
        ['search', merged.search],
        ['filter', merged.filter],
      ];

      for (const [key, value] of entries) {
        if (value === null || value === '') next.delete(key);
        else next.set(key, String(value));
      }

      const query = next.toString();
      router.replace(query ? `${pathname}?${query}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams, state],
  );

  /** Cycles a column through ascending, descending, then unsorted. */
  const toggleSort = useCallback(
    (field: string) => {
      const current = state.sort;
      if (current === `${field}:asc`) apply({ sort: `${field}:desc` });
      else if (current === `${field}:desc`) apply({ sort: null });
      else apply({ sort: `${field}:asc` });
    },
    [apply, state.sort],
  );

  const queryString = useMemo(() => {
    const params = new URLSearchParams();
    params.set('page', String(state.page));
    params.set('pageSize', String(state.pageSize));
    if (state.sort) params.set('sort', state.sort);
    if (state.search) params.set('search', state.search);
    if (state.filter) params.set('filter', state.filter);
    return params.toString();
  }, [state]);

  return { state, apply, toggleSort, queryString };
}
