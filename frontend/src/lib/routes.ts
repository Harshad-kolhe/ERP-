/**
 * Where a signed-in user lands when no explicit destination was requested.
 *
 * The dashboard, not a list screen. Landing someone directly in Parts assumes
 * every user's job starts there, which is true for nobody outside engineering.
 *
 * `/` is the public project overview rather than the dashboard, because it has to
 * render for someone who has not signed in — and for someone whose API is not
 * running yet.
 */
export const APP_HOME = '/home';

/**
 * `returnUrl` is attacker-controllable and is fed straight to `window.location`,
 * so it must be a path on this site. Testing for a leading "/" alone is not
 * enough: "//evil.example" is protocol-relative, and the browser reads it as
 * another origin.
 */
export function safeReturnUrl(value: string | string[] | undefined): string {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//')
    ? value
    : APP_HOME;
}
