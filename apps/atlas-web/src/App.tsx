import { useState } from "react";
import { Atlas } from "./Atlas";
import { CardList } from "./CardList";
import { CardDetail } from "./CardDetail";
import { SetList } from "./SetList";
import { SymbolsProvider } from "./ManaCost";

type View = "atlas" | "cards" | "sets";

export function App() {
  const [view, setView] = useState<View>("atlas");
  const [selectedId, setSelectedId] = useState<string | null>(null);

  return (
    <SymbolsProvider>
      <div className="layout">
        <div className="header">
          <div>
            <h1>Magic Atlas</h1>
            <small>Scryfall oracle catalog · GraphQL via Trax</small>
          </div>
          <nav className="tabs">
            <button
              className={view === "atlas" ? "active" : ""}
              onClick={() => { setView("atlas"); setSelectedId(null); }}
            >
              Atlas
            </button>
            <button
              className={view === "cards" ? "active" : ""}
              onClick={() => { setView("cards"); setSelectedId(null); }}
            >
              Cards
            </button>
            <button
              className={view === "sets" ? "active" : ""}
              onClick={() => { setView("sets"); setSelectedId(null); }}
            >
              Sets
            </button>
          </nav>
        </div>

        {view === "atlas" && <Atlas />}
        {view === "cards" &&
          (selectedId ? (
            <CardDetail
              key={selectedId}
              id={selectedId}
              onBack={() => setSelectedId(null)}
              onSelect={setSelectedId}
            />
          ) : (
            <CardList onSelect={setSelectedId} />
          ))}
        {view === "sets" && <SetList />}
      </div>
    </SymbolsProvider>
  );
}
