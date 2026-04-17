import { useMemo } from "react";
import { useQuery } from "@apollo/client";
import { ATLAS_POINTS_QUERY, CARDS_BY_IDS_QUERY } from "./queries";

type Point = { cardId: string; x: number; y: number; textType: string };

type AtlasResponse = {
  discover: { atlas: { atlasPointRows: { nodes: Point[] } } };
};

type CardLite = {
  id: string;
  name: string;
  typeLine: string | null;
  manaCost: string | null;
  imageUriNormal: string | null;
};

type CardsByIdsResponse = {
  discover: { atlas: { cardRows: { nodes: CardLite[] } } };
};

const TEXT_TYPE_ORDER = ["keyword", "named_triggered", "triggered", "activated", "passive"];
const K = 8;

/**
 * Finds the K nearest cards per text type for a given card. Distance is
 * squared-euclidean in UMAP (x, y) space. If the target card has multiple
 * fragments of the same type, a candidate's score is its minimum distance
 * to any of the target's fragments of that type (a card with a "Flying"
 * fragment close to this card's "Flying" fragment ranks by that pairing).
 */
function findNeighbors(
  points: Point[],
  targetCardId: string
): Record<string, string[]> {
  const targets = points.filter((p) => p.cardId === targetCardId);
  if (targets.length === 0) return {};

  const byType: Record<string, string[]> = {};
  for (const type of TEXT_TYPE_ORDER) {
    const seeds = targets.filter((p) => p.textType === type);
    if (seeds.length === 0) continue;

    // Map<cardId, min squared-distance to any seed of this type>.
    const scored = new Map<string, number>();
    for (const p of points) {
      if (p.cardId === targetCardId || p.textType !== type) continue;
      let best = Infinity;
      for (const seed of seeds) {
        const dx = p.x - seed.x;
        const dy = p.y - seed.y;
        const d = dx * dx + dy * dy;
        if (d < best) best = d;
      }
      const prior = scored.get(p.cardId);
      if (prior === undefined || best < prior) scored.set(p.cardId, best);
    }

    byType[type] = [...scored.entries()]
      .sort((a, b) => a[1] - b[1])
      .slice(0, K)
      .map(([id]) => id);
  }
  return byType;
}

export function NearestCards({
  cardId,
  onSelect,
}: {
  cardId: string;
  onSelect: (id: string) => void;
}) {
  const { data: atlasData, loading: atlasLoading } =
    useQuery<AtlasResponse>(ATLAS_POINTS_QUERY);

  const neighborsByType = useMemo(() => {
    const pts = atlasData?.discover.atlas.atlasPointRows.nodes ?? [];
    return findNeighbors(pts, cardId);
  }, [atlasData, cardId]);

  const allNeighborIds = useMemo(
    () => [...new Set(Object.values(neighborsByType).flat())],
    [neighborsByType]
  );

  const { data: cardsData, loading: cardsLoading } = useQuery<CardsByIdsResponse>(
    CARDS_BY_IDS_QUERY,
    { variables: { ids: allNeighborIds }, skip: allNeighborIds.length === 0 }
  );

  const cardMap = useMemo(() => {
    const m = new Map<string, CardLite>();
    for (const c of cardsData?.discover.atlas.cardRows.nodes ?? []) m.set(c.id, c);
    return m;
  }, [cardsData]);

  if (atlasLoading) {
    return <p style={{ color: "#9aa3bd" }}>Loading embedding neighbors…</p>;
  }
  if (Object.keys(neighborsByType).length === 0) {
    return null;
  }

  return (
    <section className="neighbors">
      <h3>Nearest cards by ability type</h3>
      {TEXT_TYPE_ORDER.map((type) => {
        const ids = neighborsByType[type];
        if (!ids || ids.length === 0) return null;
        return (
          <div key={type} className="neighbor-group">
            <h4>{type.replace(/_/g, " ")}</h4>
            <div className="neighbor-row">
              {ids.map((id) => {
                const card = cardMap.get(id);
                return (
                  <button
                    key={id}
                    type="button"
                    className="neighbor-card"
                    onClick={() => onSelect(id)}
                    disabled={!card}
                    title={card?.name ?? "Loading…"}
                  >
                    {card?.imageUriNormal ? (
                      <img src={card.imageUriNormal} alt={card.name} loading="lazy" />
                    ) : (
                      <div className="neighbor-placeholder">
                        {cardsLoading ? "…" : "?"}
                      </div>
                    )}
                    <span>{card?.name ?? "…"}</span>
                  </button>
                );
              })}
            </div>
          </div>
        );
      })}
    </section>
  );
}
