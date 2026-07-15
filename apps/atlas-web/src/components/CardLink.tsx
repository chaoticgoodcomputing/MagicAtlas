// A card name rendered as an inline, clickable link to its card page.
//
// Navigation is hash-only: setting `window.location.hash` fires the `hashchange`
// listener in App, which routes to the Card Explorer page for this name. Styling is
// deliberately subtle — the link inherits its surrounding text colour/size and
// only reveals itself (accent colour + underline) on hover/focus — so it drops
// in wherever a bare name string sits today without disturbing layout.

import type { ReactNode } from "react";

export function cardHref(name: string): string {
  return `#/card/${encodeURIComponent(name)}`;
}

export function CardLink({ name, children }: { name: string; children?: ReactNode }) {
  return (
    <a
      className="card-link"
      href={cardHref(name)}
      title={name}
      onClick={(e) => {
        // Let modified clicks (new tab, etc.) and the anchor's own href do the
        // work; for a plain click, drive the hash ourselves so nested handlers
        // (e.g. a draggable SVG node) still see a clean, single navigation.
        if (e.defaultPrevented || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        e.preventDefault();
        window.location.hash = `/card/${encodeURIComponent(name)}`;
      }}
    >
      {children ?? name}
    </a>
  );
}
