import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Merges class names, with later Tailwind utilities winning over earlier ones.
 * `clsx` alone would leave both `p-2` and `p-4` in the string and let source
 * order in the stylesheet decide, which is not what the caller means.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
