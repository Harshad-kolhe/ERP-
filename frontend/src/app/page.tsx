import { ArrowRight, Check } from 'lucide-react';
import type { Metadata } from 'next';
import { cookies } from 'next/headers';
import Link from 'next/link';

import { Card, Section, StatusPill } from '@/components/landing/primitives';
import { BrandMark } from '@/components/shell/brand-mark';
import { ThemeToggle } from '@/components/theme-toggle';
import { Button } from '@/components/ui/button';
import {
  ACCESS,
  AREAS,
  AVAILABILITY,
  JOURNEY,
  OUTCOMES,
  PROJECT,
  VALUE_PROPS,
} from '@/config/project';
import { SESSION_COOKIE } from '@/lib/api/server';
import { APP_HOME } from '@/lib/routes';

export const metadata: Metadata = {
  title: 'ERP — what the system does',
  description:
    'One system from customer enquiry to dispatched machine: what each department can do with it, and what is ready to use today.',
};

/**
 * The public front door, and the first thing the app shows.
 *
 * Deliberately outside the `(app)` group, so it renders with no session: someone
 * who has been told "the new system is at this address" arrives here, learns what
 * it is for, and finds the way in — rather than meeting a password box that
 * explains nothing.
 *
 * It describes the work, not the build. A storekeeper does not need to know what
 * the API is written in, and telling them anyway is how internal software
 * acquires its reputation for being written at people rather than for them.
 */
export default async function LandingPage() {
  // Presence of the session cookie only, to pick which call to action to show.
  // Not a security decision — the app layout resolves the real session and the
  // API re-checks every request — and deliberately not `getSession()`, because
  // this page must render when the API is unreachable.
  const signedIn = (await cookies()).has(SESSION_COOKIE);

  return (
    <div className="flex min-h-svh flex-col">
      <header className="bg-background/85 sticky top-0 z-20 border-b backdrop-blur">
        <div className="mx-auto flex h-14 max-w-7xl items-center gap-3 px-6">
          <span className="flex items-center gap-2 text-sm font-semibold tracking-tight">
            <BrandMark />
            {PROJECT.name}
          </span>
          <div className="ml-auto flex items-center gap-1.5">
            <ThemeToggle />
            <Button asChild size="sm">
              <Link href={signedIn ? APP_HOME : '/login'}>
                {signedIn ? 'Open the app' : 'Sign in'}
                <ArrowRight className="size-3.5" aria-hidden />
              </Link>
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-7xl flex-1 px-6">
        <Hero />

        <Section
          id="path"
          index="01 · The path an order takes"
          title="One order, from enquiry to delivery"
          lede="Every department below works on the same order as it moves. Nothing is re-entered at a handover, and each stage can see what the stage before it decided."
        >
          <ol className="space-y-px">
            {JOURNEY.map((step, index) => (
              <li
                key={step.stage}
                className="bg-card flex gap-4 border p-4 first:rounded-t-md last:rounded-b-md sm:gap-5"
              >
                <span className="text-muted-foreground w-5 shrink-0 pt-0.5 font-mono text-[11px] tabular-nums">
                  {String(index + 1).padStart(2, '0')}
                </span>
                <div className="min-w-0">
                  <p className="flex flex-wrap items-baseline gap-x-2.5">
                    <span className="text-[13.5px] font-semibold">{step.stage}</span>
                    <span className="text-muted-foreground font-mono text-[10.5px] tracking-[0.08em] uppercase">
                      {step.who}
                    </span>
                  </p>
                  <p className="text-muted-foreground mt-1 text-[13px] leading-relaxed">
                    {step.what}
                  </p>
                </div>
              </li>
            ))}
          </ol>
        </Section>

        <Section
          id="areas"
          index="02 · What you can do"
          title="By department"
          lede="The full scope of the system, area by area. Each one is marked with whether it is ready today, being built now, or still planned — so nothing here promises a screen you cannot find."
        >
          {/* A third column past 1280px. The container widened from max-w-5xl,
              which left ~450px of dead margin on each side of a 1080p screen; two
              columns at the new width would just be two very wide cards. Prose
              blocks keep their own max-w-3xl, which is the real reason the page
              is not simply full-bleed. */}
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {AREAS.map((area) => {
              const Icon = area.icon;

              return (
                <div key={area.name} className="bg-card flex flex-col rounded-md border p-5">
                  <div className="mb-2 flex items-start gap-2.5">
                    <Icon className="text-primary mt-0.5 size-4 shrink-0" aria-hidden />
                    <h3 className="text-[13.5px] font-semibold">{area.name}</h3>
                    <span className="ml-auto">
                      <StatusPill status={area.status} />
                    </span>
                  </div>
                  <p className="text-muted-foreground mb-3 text-[13px] leading-relaxed">
                    {area.summary}
                  </p>
                  <ul className="mt-auto space-y-1.5">
                    {area.does.map((item) => (
                      <li key={item} className="flex gap-2 text-[12.5px] leading-relaxed">
                        <Check className="text-primary/70 mt-[3px] size-3 shrink-0" aria-hidden />
                        <span className="text-muted-foreground">{item}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              );
            })}
          </div>
        </Section>

        <Section
          id="why"
          index="03 · Where it helps"
          title="What changes day to day"
          lede="Not features — the difference they make to someone who has been doing this job with a spreadsheet and a phone."
        >
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {OUTCOMES.map((outcome) => (
              <Card key={outcome.title} title={outcome.title}>
                {outcome.body}
              </Card>
            ))}
          </div>
        </Section>

        <Section
          id="today"
          index="04 · Where it stands"
          title="What you can use today"
          lede="Stated plainly, because a system that is vague about its own gaps gets used on trust once and then abandoned."
        >
          <div className="grid gap-4 lg:grid-cols-3">
            {AVAILABILITY.map((group) => (
              <div key={group.heading} className="bg-card rounded-md border p-5">
                <div className="mb-1.5 flex items-center gap-2">
                  <h3 className="text-[13.5px] font-semibold">{group.heading}</h3>
                  <StatusPill status={group.status} />
                </div>
                <p className="text-muted-foreground mb-3 text-[12px] leading-relaxed">
                  {group.note}
                </p>
                <ul className="space-y-1.5">
                  {group.items.map((item) => (
                    <li
                      key={item}
                      className="text-muted-foreground border-l-2 pl-2.5 text-[12.5px] leading-relaxed"
                    >
                      {item}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </Section>

        <Section
          id="access"
          index="05 · Getting started"
          title="How to get in, and what to do if something is missing"
        >
          <div className="grid gap-4 sm:grid-cols-3">
            {ACCESS.map((step, index) => (
              <Card key={step.title} eyebrow={`Step ${index + 1}`} title={step.title}>
                {step.body}
              </Card>
            ))}
          </div>

          <div className="mt-5">
            <Button asChild>
              <Link href={signedIn ? APP_HOME : '/login'}>
                {signedIn ? 'Open the app' : 'Sign in'}
                <ArrowRight className="size-3.5" aria-hidden />
              </Link>
            </Button>
          </div>
        </Section>
      </main>

      <footer className="border-t py-8">
        <div className="text-muted-foreground mx-auto max-w-7xl px-6 text-[12px]">
          <p>
            {PROJECT.name} — internal system for engineer-to-order machine manufacturing. Technical
            documentation lives in <code className="font-mono">docs/</code>.
          </p>
        </div>
      </footer>
    </div>
  );
}

/** The one section that is layout rather than content, so it stays in this file. */
function Hero() {
  return (
    <div className="auth-canvas relative -mx-6 overflow-hidden px-6 py-14 sm:py-20">
      <div className="relative z-10">
        <p className="text-muted-foreground mb-3 font-mono text-[11px] tracking-[0.18em] uppercase">
          {PROJECT.name} · Engineer-to-order machine manufacturing
        </p>
        <h1 className="max-w-3xl text-3xl font-semibold tracking-tight sm:text-4xl">
          {PROJECT.tagline}
        </h1>
        <p className="mt-5 max-w-2xl text-sm leading-relaxed">{PROJECT.summary}</p>
        <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-relaxed">
          {PROJECT.purpose}
        </p>

        <div className="mt-7 flex flex-wrap gap-2">
          <Button asChild variant="outline" size="sm">
            <Link href="#path">How an order flows</Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href="#areas">What each department can do</Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href="#today">What is ready today</Link>
          </Button>
        </div>

        <div className="mt-10 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {VALUE_PROPS.map((prop) => (
            <div key={prop.title} className="bg-card/70 rounded-md border p-4 backdrop-blur-sm">
              <p className="text-[13px] font-semibold">{prop.title}</p>
              <p className="text-muted-foreground mt-1.5 text-[12px] leading-relaxed">
                {prop.body}
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
