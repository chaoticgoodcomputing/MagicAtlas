// The one live-data companion to the concept surfaces: the UMAP embedding of
// every parsed ability fragment (atlasPointRows), rendered with regl-scatterplot
// against the real GraphQL API. This is the raw material the family/port model
// is built on — the parser's-eye view of the corpus.

import { SectionHead } from "../components/primitives";
import { SymbolsProvider } from "../ManaCost";
import { Atlas } from "../Atlas";

export function Embedding() {
  // Scope the mana-symbol provider (and its GraphQL query) to this live view so
  // the concept surfaces stay free of any API dependency.
  return (
    <SymbolsProvider>
      <div className="view-grid">
        <SectionHead kicker="Reference · live data" title="Ability Embedding">
          Every parsed ability fragment as a point in the oracle-text embedding, coloured by ability kind.
          This view queries the live <code>atlas-api</code> (GraphQL); the concept surfaces above run on the
          sample corpus until the port/family datasets get an API surface.
        </SectionHead>
        <div className="panel">
          <Atlas />
        </div>
      </div>
    </SymbolsProvider>
  );
}
