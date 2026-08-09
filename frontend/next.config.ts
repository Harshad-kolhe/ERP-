import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  reactStrictMode: true,

  // The browser never talks to the .NET API directly; it goes through the BFF
  // route handler at /api/[...path]. This value is read server-side only and is
  // deliberately not exposed via NEXT_PUBLIC_.
  env: {},

  async headers() {
    const isDevelopment = process.env.NODE_ENV !== 'production';

    // React's development build calls eval() to reconstruct callstacks and map
    // sources, and Turbopack's HMR client does the same. Neither happens in a
    // production build — React never calls eval() there — so the allowance is
    // scoped to development rather than shipped.
    const scriptSrc = isDevelopment
      ? "script-src 'self' 'unsafe-inline' 'unsafe-eval'"
      : "script-src 'self' 'unsafe-inline'";

    // The dev server talks to itself over a websocket for hot reload.
    const connectSrc = isDevelopment ? "connect-src 'self' ws: wss:" : "connect-src 'self'";

    return [
      {
        source: '/:path*',
        headers: [
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'X-Frame-Options', value: 'DENY' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          {
            // No third-party script may load. The legacy app pulled jQuery,
            // Bootstrap and a floating `jszip@3` from three different CDNs with
            // no subresource integrity, which is a live supply-chain exposure.
            //
            // `'unsafe-inline'` remains in both environments because Next.js
            // injects inline bootstrap scripts. Removing it needs per-request
            // nonces threaded through proxy.ts and the document — worth doing
            // before this is exposed beyond the office, and noted in docs/status.md.
            key: 'Content-Security-Policy',
            value: [
              "default-src 'self'",
              scriptSrc,
              "style-src 'self' 'unsafe-inline'",
              "img-src 'self' data: blob:",
              connectSrc,
              "font-src 'self' data:",
              "object-src 'none'",
              "base-uri 'self'",
              "form-action 'self'",
              "frame-ancestors 'none'",
            ].join('; '),
          },
        ],
      },
    ];
  },
};

export default nextConfig;
