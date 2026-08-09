import { ApiError, isProblemDetails, type ProblemDetails } from './problem-details';

/**
 * The single entry point for talking to the API.
 *
 * Always relative to this origin, so every call travels through the BFF proxy
 * and the session cookie rides along automatically. There is no base URL to
 * configure and no token to attach in browser code.
 */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/v1${path}`, {
    ...init,
    headers: {
      accept: 'application/json',
      ...(init?.body ? { 'content-type': 'application/json' } : {}),
      ...init?.headers,
    },
    credentials: 'same-origin',
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const payload: unknown = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    // Status codes are meaningful here, so this is a real branch rather than an
    // inspection of a `Status: false` field inside a 200 response.
    throw new ApiError(
      isProblemDetails(payload)
        ? payload
        : ({
            type: 'https://problems.erp/unexpected',
            title: 'Request failed',
            status: response.status,
          } satisfies ProblemDetails),
    );
  }

  return payload as T;
}
