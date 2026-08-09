/**
 * Where the sidebar remembers whether it is collapsed.
 *
 * A cookie rather than `localStorage`, because the server renders the shell: the
 * width has to be known while the HTML is being built. `localStorage` is only
 * readable after hydration, which is a frame too late — the sidebar would paint
 * wide and then snap narrow on every single page load.
 *
 * It carries no session meaning, so it is deliberately not `httpOnly`: the client
 * has to write it when the toggle is clicked.
 */
export const SIDEBAR_COOKIE = 'sidebar_collapsed';

/** A year. A layout preference that expires is a preference nobody trusts. */
export const SIDEBAR_COOKIE_MAX_AGE = 60 * 60 * 24 * 365;
