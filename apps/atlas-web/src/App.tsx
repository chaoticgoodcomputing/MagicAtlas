import { useState } from "react";
import { CardList } from "./CardList";
import { CardDetail } from "./CardDetail";

export function App() {
  const [selectedId, setSelectedId] = useState<string | null>(null);

  return (
    <div className="layout">
      <div className="header">
        <h1>Magic Atlas</h1>
        <small>Scryfall oracle catalog · GraphQL via Trax</small>
      </div>

      {selectedId ? (
        <CardDetail id={selectedId} onBack={() => setSelectedId(null)} />
      ) : (
        <CardList onSelect={setSelectedId} />
      )}
    </div>
  );
}
