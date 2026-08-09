import { Loader2Icon } from "lucide-react"

import { cn } from "@/lib/utils"

/**
 * A spinning icon, and nothing else.
 *
 * Deliberately `aria-hidden`: announcing the *region* is the caller's job, not the
 * icon's. With `role="status" aria-label="Loading"` baked in, every button that
 * shows one while submitting announced "Loading Signing in" — the icon talking
 * over the label — and a wrapper that was already a status region ended up with
 * two nested ones.
 *
 * Inside a button, nothing extra is needed: the label already changes from "Save"
 * to "Saving". Somewhere a whole panel is loading, put `role="status"` on the
 * container that holds the text.
 */
function Spinner({ className, ...props }: React.ComponentProps<"svg">) {
  return (
    <Loader2Icon
      aria-hidden
      className={cn("size-4 animate-spin", className)}
      {...props}
    />
  )
}

export { Spinner }
