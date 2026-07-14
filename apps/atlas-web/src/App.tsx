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

export type ViewKey =
  | "overview" | "metro" | "station" | "explorer"
  | "deck" | "synergy" | "design" | "embedding";

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

function readHash(): ViewKey {
  const h = window.location.hash.replace(/^#\/?/, "") as ViewKey;
  return NAV.some((n) => n.key === h) ? h : "overview";
}

export function App() {
  const [view, setView] = useState<ViewKey>(readHash);

  // Inject the family hues as CSS variables once, and keep the view in sync
  // with the URL hash so every surface is deep-linkable.
  useEffect(() => { applyFamilyVars(); }, []);
  useEffect(() => {
    const onHash = () => setView(readHash());
    window.addEventListener("hashchange", onHash);
    return () => window.removeEventListener("hashchange", onHash);
  }, []);

  const navigate = (v: ViewKey) => {
    window.location.hash = `/${v}`;
    setView(v);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

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
