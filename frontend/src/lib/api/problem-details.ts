/**
 * RFC 9457 problem responses, as the client sees them.
 *
 * The API always signals failure with a 4xx/5xx status and this body. Clients
 * branch on `code` — a stable identifier that is part of the contract — never on
 * `detail`, which is human-facing text that may be reworded at any time.
 */
export interface ProblemDetails {
  type: string;
  title: string;
  detail?: string;
  status: number;
  /** Stable error identifier, e.g. `part.number.duplicate`. */
  code?: string;
  /** Correlates with the server log entry. Show this in support dialogs. */
  traceId?: string;
  /** Present on validation failures: field name to messages. */
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(readonly problem: ProblemDetails) {
    super(problem.detail ?? problem.title);
    this.name = 'ApiError';
  }

  get isValidation(): boolean {
    return this.problem.status === 400 && this.problem.errors !== undefined;
  }

  get isConflict(): boolean {
    return this.problem.status === 409;
  }

  get isForbidden(): boolean {
    return this.problem.status === 403;
  }

  /** True when the row changed underneath the user and they must reload. */
  get isStale(): boolean {
    return this.problem.code === 'part.stale_row_version';
  }
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'title' in value
  );
}
