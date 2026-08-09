'use client';

import { useLinkStatus } from 'next/link';

/**
 * A dot that appears on a nav link while its screen is on its way.
 *
 * Must be rendered *inside* a `<Link>` — that is where `useLinkStatus` reads from.
 *
 * The route-level answer is `(app)/loading.tsx`, and this is the smaller
 * companion to it: the skeleton says "a screen is coming", this says "the one you
 * just clicked". Only on the sidebar, because those are the links people click
 * without looking at the page.
 *
 * Note it will often never appear, which is correct rather than broken: a
 * prefetched route skips the pending phase entirely, so in production this shows
 * up only on the navigations that are genuinely slow — the ones worth confirming.
 */
export function NavPending() {
  const { pending } = useLinkStatus();

  return <span aria-hidden className={`nav-hint${pending ? ' is-pending' : ''}`} />;
}
