import {
  Boxes,
  CalendarRange,
  ChartNoAxesColumn,
  Cog,
  Headset,
  ShieldCheck,
  ShoppingCart,
  Truck,
  UserCog,
  Warehouse,
  type LucideIcon,
} from 'lucide-react';

/**
 * What the landing page says about this system, in one typed file.
 *
 * Written for the people who will use it — a buyer, a storekeeper, an inspector —
 * not for the people building it. So it describes the work the system does and
 * where it helps, and leaves the stack, the schema and the guardrails to
 * `docs/architecture.md`, where the audience is different.
 *
 * Every capability carries a status, and the page renders it. A service catalogue
 * that lists what is planned alongside what exists, with no distinction, is how a
 * system ends up promising screens nobody can find — so the distinction is part
 * of the data, not a footnote.
 */

export type Availability = 'available' | 'building' | 'planned';

export const PROJECT = {
  name: 'ERP',
  tagline: 'One system, from customer enquiry to dispatched machine.',
  summary:
    'Everyone working on the same order works from the same record — sales, engineering, planning, purchase, stores, production, quality and dispatch.',
  purpose:
    'Built for a business where every machine is a one-off: the specification changes, the bill of materials is the plan, and the answer to “where has this order reached?” has to be one answer rather than four people’s spreadsheets.',
} as const;

/** The four things a new user notices first. Plain claims, no numbers to defend. */
export const VALUE_PROPS = [
  {
    title: 'One shared record',
    body: 'An order, a part, a purchase order — entered once, and the same everywhere. No re-keying between departments and no private copy that quietly disagrees.',
  },
  {
    title: 'Only your work',
    body: 'What you see is matched to your job. A storekeeper does not wade through quotations, and access to anything sensitive is granted deliberately rather than by default.',
  },
  {
    title: 'Approvals that leave a trail',
    body: 'Anything that commits money or changes a design goes through approval, and the record of who approved what, and when, stays with the document.',
  },
  {
    title: 'Nothing to install',
    body: 'It runs in a browser on the shop floor, at a desk, or on a phone. No client software, no per-machine setup.',
  },
] as const;

// ---------------------------------------------------------------------------
// The path an order takes
// ---------------------------------------------------------------------------

/**
 * One order, end to end. This is the spine of the whole system: every work area
 * below is a stop on it, which is why it is stated before the areas themselves.
 */
export const JOURNEY: ReadonlyArray<{ stage: string; who: string; what: string }> = [
  {
    stage: 'Enquiry',
    who: 'Sales',
    what: 'What the customer needs, with their drawings and specifications attached to the enquiry rather than left in an inbox.',
  },
  {
    stage: 'Quotation',
    who: 'Sales',
    what: 'Price it, revise it, and send it for approval. Earlier versions stay readable, so a customer question about last month’s offer has an answer.',
  },
  {
    stage: 'Order',
    who: 'Sales',
    what: 'An accepted quotation becomes a confirmed order, and everything after this point traces back to it.',
  },
  {
    stage: 'Engineering',
    who: 'Design',
    what: 'The machine is defined as a bill of materials — assemblies, sub-assemblies and bought parts — with drawings held against the part.',
  },
  {
    stage: 'Planning',
    who: 'Planning',
    what: 'Work orders and job cards with dates, and a clear split between what stock can cover and what has to be bought.',
  },
  {
    stage: 'Purchase',
    who: 'Purchase',
    what: 'Requisitions raised from real shortages, supplier quotes compared, and purchase orders issued within approval limits.',
  },
  {
    stage: 'Receive and inspect',
    who: 'Stores, Quality',
    what: 'Goods received against the order, then inspected before they count as stock. Short, excess and rejected quantities are all recorded as what they are.',
  },
  {
    stage: 'Build',
    who: 'Production',
    what: 'Material issued to a job card, assembly progress recorded against the machine, and what is still pending visible without a phone call.',
  },
  {
    stage: 'Final inspection',
    who: 'Quality',
    what: 'The finished machine is checked and signed off, with the inspection record kept against it.',
  },
  {
    stage: 'Pack and dispatch',
    who: 'Dispatch',
    what: 'Packed into boxes with contents recorded, documentation prepared, and vehicle and delivery details confirmed.',
  },
  {
    stage: 'After delivery',
    who: 'Service',
    what: 'Service enquiries, complaints and spares handled against the machine that was actually shipped, with its own build history.',
  },
];

// ---------------------------------------------------------------------------
// What you can do
// ---------------------------------------------------------------------------

export const AREAS: ReadonlyArray<{
  name: string;
  icon: LucideIcon;
  status: Availability;
  summary: string;
  does: readonly string[];
}> = [
  {
    name: 'Sales and customer support',
    icon: Headset,
    status: 'planned',
    summary: 'From first enquiry to after-sales service, against one customer record.',
    does: [
      'Record customer enquiries with requirements and attachments',
      'Build, revise and compare quotations',
      'Send a quotation for approval before it reaches the customer',
      'Turn an accepted quotation into a confirmed order',
      'Log service enquiries, complaints and spares against a delivered machine',
    ],
  },
  {
    name: 'Engineering and bill of materials',
    icon: Boxes,
    status: 'building',
    summary: 'One part master, and a bill of materials whose history you can read.',
    does: [
      'One part master for bought, made and assembly items',
      'Build multi-level bills of materials, or copy from a similar machine and edit',
      'Revise a BOM without losing the version a machine was already built to',
      'Keep drawings and documents against the part, not in a shared folder',
      'Send a new part or a BOM change for approval',
    ],
  },
  {
    name: 'Planning',
    icon: CalendarRange,
    status: 'planned',
    summary: 'Turning an order into dated work, and knowing what will block it.',
    does: [
      'Create work orders and job cards from an order, with dates',
      'See what stock can cover and what must be bought, before work starts',
      'Reschedule and see what else moves as a result',
      'Track progress of every open work order in one list',
    ],
  },
  {
    name: 'Purchase',
    icon: ShoppingCart,
    status: 'planned',
    summary: 'Buying against real shortages, with the negotiation on record.',
    does: [
      'Raise purchase requisitions from a shortage rather than from memory',
      'Compare supplier quotations and record what was negotiated',
      'Issue purchase orders within approval limits',
      'Follow pending and overdue deliveries without chasing suppliers blind',
      'Keep supplier details, rates and history in one place',
    ],
  },
  {
    name: 'Stores and inventory',
    icon: Warehouse,
    status: 'planned',
    summary: 'Stock that matches the shelf, and every movement explained.',
    does: [
      'Receive goods against a purchase order, recording short and excess quantities',
      'Issue material to a job card and record what was actually consumed',
      'See live stock by part and by location',
      'Return unused material to stores and rejected material to the supplier',
      'Move stock between locations and business units with a record of why',
    ],
  },
  {
    name: 'Production',
    icon: Cog,
    status: 'planned',
    summary: 'Where each machine has reached, without walking the floor to find out.',
    does: [
      'Job cards per operation, with progress and time recorded',
      'Assembly status: what is fitted, what is still awaited',
      'See the machines in build and what each one is waiting for',
      'Record rework and the reason for it',
    ],
  },
  {
    name: 'Quality',
    icon: ShieldCheck,
    status: 'planned',
    summary: 'Nothing enters stock or leaves the gate unchecked.',
    does: [
      'Inspect incoming goods before they become stock',
      'Record rejections with reasons and route them back to the supplier',
      'Raise non-conformance reports and re-inspect reworked material',
      'Sign off the finished machine, with the inspection record kept against it',
    ],
  },
  {
    name: 'Dispatch',
    icon: Truck,
    status: 'planned',
    summary: 'What left, when, to whom, and exactly what was in the box.',
    does: [
      'Pack a machine into boxes with contents recorded per box',
      'Prepare delivery documentation',
      'Record vehicle and transport details',
      'Confirm dispatch, and keep what was shipped answerable later',
    ],
  },
  {
    name: 'Lists, reports and exports',
    icon: ChartNoAxesColumn,
    status: 'available',
    summary: 'Every list searchable, sortable and exportable — as shown on screen.',
    does: [
      'A pending list for every stage: what is waiting on you',
      'Search, filter, sort and group any list, and keep the arrangement',
      'Part and stock ledgers showing every movement and the document behind it',
      'Export to Excel matching exactly what is on screen',
      'Lists stay fast as the data grows, because only the page you are looking at is fetched',
    ],
  },
  {
    name: 'Users and access',
    icon: UserCog,
    status: 'available',
    summary: 'Access matched to the job, and changes attributable to a person.',
    does: [
      'Create users and assign roles',
      'Build roles from individual permissions, per screen and per action',
      'Run several business units in one system, kept separate from each other',
      'Every record carries who created it, who changed it and when',
    ],
  },
];

// ---------------------------------------------------------------------------
// Why it helps
// ---------------------------------------------------------------------------

export const OUTCOMES = [
  {
    title: 'One answer to “where has it reached?”',
    body: 'Every stage writes to the same order, so status is read rather than assembled from four people’s files.',
  },
  {
    title: 'Traceable after the fact',
    body: 'Which BOM version a machine was built to, which supplier’s batch went in, who passed the inspection — answerable months later, not reconstructed from memory.',
  },
  {
    title: 'Mistakes get caught earlier',
    body: 'The system refuses what does not add up — issuing stock that is not there, receiving against a closed order — at the point of entry rather than at month end.',
  },
  {
    title: 'Less re-keying, fewer copies',
    body: 'A purchase order carries the requisition’s detail; a receipt carries the order’s. Nobody retypes what the system already knows.',
  },
  {
    title: 'Approvals are visible, not verbal',
    body: 'What is waiting for whom is a list, and an approval leaves a record on the document instead of in a conversation.',
  },
  {
    title: 'Nobody sees more than they should',
    body: 'Rates, margins and personnel data are behind permissions, so access is a decision someone made rather than an accident of the menu.',
  },
] as const;

// ---------------------------------------------------------------------------
// What exists today
// ---------------------------------------------------------------------------

/** Deliberately in the user's words, and deliberately honest about the gaps. */
export const AVAILABILITY: ReadonlyArray<{
  status: Availability;
  heading: string;
  note: string;
  items: readonly string[];
}> = [
  {
    status: 'available',
    heading: 'Ready to use now',
    note: 'Live, in daily use, and safe to rely on.',
    items: [
      'Sign in, and your own account',
      'Part master — search, filter and export',
      'Suppliers, customers and employees',
      'Business units, kept separate from one another',
      'Roles and permissions administration',
      'Your dashboard, showing what is waiting on you',
    ],
  },
  {
    status: 'building',
    heading: 'Being built now',
    note: 'Next to arrive, in this order.',
    items: [
      'Creating and editing parts, with approval',
      'Users and role assignment',
      'Bill of materials',
      'Attaching drawings and documents',
    ],
  },
  {
    status: 'planned',
    heading: 'Planned',
    note: 'Sequenced by which department is waiting hardest, and re-ordered as that changes.',
    items: [
      'Purchase, goods receipt and stock',
      'Planning, work orders and job cards',
      'Quality and inspection',
      'Dispatch and packing',
      'Quotations and sales orders',
      'Invoicing, and a link to the accounts system',
    ],
  },
];

export const ACCESS = [
  {
    title: 'Sign in',
    body: 'Use the account your administrator created for you. If you have forgotten the password, reset it from the sign-in page.',
  },
  {
    title: 'Ask for what you need',
    body: 'If a screen you expect is missing, the permission has not been granted yet. An administrator can add it without any change to the software.',
  },
  {
    title: 'Report anything that looks wrong',
    body: 'A number that does not match the shelf, or a screen that will not accept a valid entry, is worth reporting the same day. Early reports are how this stays trustworthy.',
  },
] as const;
