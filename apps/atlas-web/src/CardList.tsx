import { useMemo, useState } from "react";
import { useQuery } from "@apollo/client";
import { CARDS_QUERY } from "./queries";

type CardNode = {
  id: string;
  name: string;
  manaCost: string | null;
  typeLine: string | null;
  rarity: string;
  cmc: number;
  colors: string[];
  imageUriNormal: string | null;
  priceUsd: number | null;
  setName: string;
  edhrecRank: number | null;
};

type CardsResponse = {
  discover: {
    atlas: {
      cardRows: {
        totalCount: number;
        pageInfo: { hasNextPage: boolean; endCursor: string | null };
        nodes: CardNode[];
      };
    };
  };
};

const RARITIES = ["common", "uncommon", "rare", "mythic", "special", "bonus"];

export function CardList({ onSelect }: { onSelect: (id: string) => void }) {
  const [nameSearch, setNameSearch] = useState("");
  const [rarity, setRarity] = useState("");
  const [cursor, setCursor] = useState<string | null>(null);
  const [cursorStack, setCursorStack] = useState<string[]>([]);

  const where = useMemo(() => {
    const conditions: Record<string, unknown> = {};
    if (nameSearch) conditions.name = { contains: nameSearch };
    if (rarity) conditions.rarity = { eq: rarity };
    return Object.keys(conditions).length ? conditions : undefined;
  }, [nameSearch, rarity]);

  const { data, loading, error } = useQuery<CardsResponse>(CARDS_QUERY, {
    variables: {
      first: 30,
      after: cursor,
      where,
      order: [{ edhrecRank: "ASC" }],
    },
  });

  const cards = data?.discover.atlas.cardRows;

  return (
    <>
      <div className="filters">
        <input
          type="text"
          placeholder="Search by name..."
          value={nameSearch}
          onChange={(e) => {
            setNameSearch(e.target.value);
            setCursor(null);
            setCursorStack([]);
          }}
        />
        <select
          value={rarity}
          onChange={(e) => {
            setRarity(e.target.value);
            setCursor(null);
            setCursorStack([]);
          }}
        >
          <option value="">All rarities</option>
          {RARITIES.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>
        <span style={{ alignSelf: "center", color: "#9aa3bd" }}>
          {cards ? `${cards.totalCount.toLocaleString()} cards` : ""}
        </span>
      </div>

      {loading && <p>Loading…</p>}
      {error && <p style={{ color: "#f77" }}>Error: {error.message}</p>}

      <div className="grid">
        {cards?.nodes.map((c) => (
          <div key={c.id} className="card" onClick={() => onSelect(c.id)}>
            {c.imageUriNormal ? (
              <img src={c.imageUriNormal} alt={c.name} loading="lazy" />
            ) : (
              <div style={{ aspectRatio: "488/680", background: "#0f1120" }} />
            )}
            <div className="meta">
              <h3>{c.name}</h3>
              <span>
                {c.typeLine ?? "—"} · {c.rarity}
              </span>
            </div>
          </div>
        ))}
      </div>

      <div className="pager">
        <button
          disabled={cursorStack.length === 0}
          onClick={() => {
            const prev = [...cursorStack];
            prev.pop();
            setCursor(prev.length ? prev[prev.length - 1] : null);
            setCursorStack(prev);
          }}
        >
          ← Prev
        </button>
        <button
          disabled={!cards?.pageInfo.hasNextPage}
          onClick={() => {
            if (cards?.pageInfo.endCursor) {
              setCursorStack([...cursorStack, cards.pageInfo.endCursor]);
              setCursor(cards.pageInfo.endCursor);
            }
          }}
        >
          Next →
        </button>
      </div>
    </>
  );
}
