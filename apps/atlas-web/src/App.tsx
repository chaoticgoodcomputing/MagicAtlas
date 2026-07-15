import { useEffect, useState } from "react";
import { applyFamilyVars } from "./data/mock";
import Overview from "./views/Overview";
import MetroMap from "./views/MetroMap";
import StationFocus from "./views/StationFocus";
import CardExplorer from "./views/CardExplorer";
import DeckLens from "./views/DeckLens";
import SynergyWeb from "./views/SynergyWeb";
import DesignSystem from "./views/DesignSystem";
import { Embedding } from "./views/Embedding";
import CardPage from "./views/CardPage";

export type ViewKey =
  | "overview" | "metro" | "station" | "explorer"
  | "deck" | "synergy" | "design" | "embedding";

/** The two hash shapes: a flat concept view (`#/overview`) or a per-card page
 *  (`#/card/<url-encoded-name>`). A card page shows no active nav tab. */
type Route = { kind: "view"; view: ViewKey } | { kind: "card"; name: string };

interface NavItem { key: ViewKey; label: string; group: "explore" | "exploit" | "reference"; }

const NAV: NavItem[] = [
  { key: "overview", label: "Overview", group: "explore" },
  { key: "metro", label: "Metro", group: "explore" },
  { key: "station", label: "Station", group: "explore" },
  { key: "explorer", label: "Card Explorer", group: "explore" },
  { key: "deck", label: "Deck Lens", group: "exploit" },
  { key: "synergy", label: "Synergy Web", group: "exploit" },
  { key: "embedding", label: "Embedding", group: "reference" },
  { key: "design", label: "Design", group: "reference" },
];

function readHash(): Route {
  const raw = window.location.hash.replace(/^#\/?/, "");
  if (raw.startsWith("card/")) {
    return { kind: "card", name: decodeURIComponent(raw.slice("card/".length)) };
  }
  const view = raw as ViewKey;
  return { kind: "view", view: NAV.some((n) => n.key === view) ? view : "overview" };
}

export function App() {
  const [route, setRoute] = useState<Route>(readHash);

  // Inject the family hues as CSS variables once, and keep the route in sync
  // with the URL hash so every surface (views + card pages) is deep-linkable
  // and back/forward-navigable.
  useEffect(() => { applyFamilyVars(); }, []);
  useEffect(() => {
    const onHash = () => {
      setRoute(readHash());
      window.scrollTo({ top: 0, behavior: "smooth" });
    };
    window.addEventListener("hashchange", onHash);
    return () => window.removeEventListener("hashchange", onHash);
  }, []);

  const navigate = (v: ViewKey) => {
    window.location.hash = `/${v}`;
    setRoute({ kind: "view", view: v });
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const view = route.kind === "view" ? route.view : null;

  return (
    <div className="atlas-shell">
      <nav className="atlas-nav">
        <div className="brand">
          Magic Atlas
          <small>interaction-structure explorer</small>
        </div>
        <div className="nav-links">
          {NAV.map((n) => (
            <button
              key={n.key}
              className={view === n.key ? "active" : ""}
              onClick={() => navigate(n.key)}
            >
              {n.label}
            </button>
          ))}
        </div>
      </nav>

      <main className="atlas-main">
        {route.kind === "card" && <CardPage name={route.name} onNavigate={navigate} />}
        {view === "overview" && <Overview onNavigate={navigate} />}
        {view === "metro" && <MetroMap />}
        {view === "station" && <StationFocus />}
        {view === "explorer" && <CardExplorer />}
        {view === "deck" && <DeckLens />}
        {view === "synergy" && <SynergyWeb />}
        {view === "embedding" && <Embedding />}
        {view === "design" && <DesignSystem />}
      </main>
    </div>
  );
}
