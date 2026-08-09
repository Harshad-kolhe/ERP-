import { GridSkeleton } from '@/features/masters/shared/master-list-screen';

/**
 * The route-transition fallback for every signed-in screen.
 *
 * This is the real fix for "clicking a menu item does nothing for a second": with
 * no `loading.tsx`, Next holds the old page on screen until the new one's data
 * resolves, so the click has no visible effect at all.
 *
 * It reuses `GridSkeleton` so a route transition and a Suspense boundary inside a
 * screen produce the same silhouette — two different shapes for the same wait is
 * how a fallback ends up reading as a layout bug.
 */
export default function AppLoading() {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="border-border h-[69px] shrink-0 border-b" />
      <div className="flex min-h-0 flex-1 flex-col p-4">
        <GridSkeleton />
      </div>
    </div>
  );
}
