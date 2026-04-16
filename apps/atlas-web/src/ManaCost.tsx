import { useQuery } from "@apollo/client";
import { createContext, useContext, useMemo, type ReactNode } from "react";
import { SYMBOLS_QUERY } from "./queries";

type SymbolMap = Record<string, { svgUri: string; english: string }>;

const SymbolsContext = createContext<SymbolMap>({});

export function SymbolsProvider({ children }: { children: ReactNode }) {
  const { data } = useQuery<{
    discover: { atlas: { cardSymbolRows: { nodes: { symbol: string; svgUri: string; english: string }[] } } };
  }>(SYMBOLS_QUERY);

  const map = useMemo<SymbolMap>(() => {
    const out: SymbolMap = {};
    for (const s of data?.discover.atlas.cardSymbolRows.nodes ?? []) {
      out[s.symbol] = { svgUri: s.svgUri, english: s.english };
    }
    return out;
  }, [data]);

  return <SymbolsContext.Provider value={map}>{children}</SymbolsContext.Provider>;
}

/**
 * Renders a Scryfall mana-cost / reminder string like "{2}{W}{U}" as inline SVG pips.
 * Unknown tokens fall through as plain text (e.g. mid-text braces).
 */
export function ManaCost({ value }: { value: string | null | undefined }) {
  const symbols = useContext(SymbolsContext);
  if (!value) return null;
  const parts = value.match(/\{[^}]+\}|[^{}]+/g) ?? [];
  return (
    <span className="mana-cost">
      {parts.map((part, i) => {
        if (part.startsWith("{") && symbols[part]) {
          const s = symbols[part];
          return (
            <img
              key={i}
              src={s.svgUri}
              alt={s.english}
              title={s.english}
              className="mana-pip"
            />
          );
        }
        return <span key={i}>{part}</span>;
      })}
    </span>
  );
}
