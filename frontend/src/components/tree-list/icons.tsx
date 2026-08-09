/**
 * The icon set `TreeList` draws from, mapped onto lucide.
 *
 * The grid was written against a hand-rolled icon module in the prototype. This
 * shim keeps the component's import list unchanged while lucide stays the single
 * icon source in this app — a second icon library would be the more obvious cost,
 * but the real one is two sets of glyphs that drift apart at different stroke
 * weights.
 */
export {
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Download,
  Filter,
  Search,
  Columns3 as Columns,
  Rows3 as Rows,
  X as Close,
} from 'lucide-react';
