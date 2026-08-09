import { Factory } from 'lucide-react';

import { cn } from '@/lib/utils';

/**
 * The product mark, in the three places the app names itself: the sidebar, the
 * landing header and the sign-in frame.
 *
 * It replaces a bare `bg-primary` dot that each of those three had inlined. A dot
 * is not a mark — it reads as a status light, and it was carrying the whole job in
 * the collapsed sidebar, where the wordmark is `sr-only` and the mark is the only
 * thing on screen.
 *
 * The tile treatment is the one `MasterPageHeader` already uses for a master's
 * icon, so the app's two "this is what you are looking at" badges are the same
 * object at two sizes.
 */
export function BrandMark({ className }: { className?: string }) {
  return (
    <span
      aria-hidden
      className={cn(
        'from-primary to-primary/70 text-primary-foreground flex size-6 shrink-0',
        'items-center justify-center rounded-md bg-gradient-to-br',
        className,
      )}
    >
      <Factory className="size-3.5" />
    </span>
  );
}
