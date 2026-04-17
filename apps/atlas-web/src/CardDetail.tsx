import { useQuery } from "@apollo/client";
import { CARD_QUERY } from "./queries";
import { ManaCost } from "./ManaCost";
import { Rulings } from "./Rulings";
import { NearestCards } from "./NearestCards";

type CardDetailNode = {
  id: string;
  name: string;
  oracleId: string | null;
  manaCost: string | null;
  typeLine: string | null;
  oracleText: string | null;
  rarity: string;
  cmc: number;
  power: string | null;
  toughness: string | null;
  loyalty: string | null;
  colors: string[];
  colorIdentity: string[];
  keywords: string[];
  imageUriLarge: string | null;
  scryfallUri: string;
  setName: string;
  set: string;
  artist: string | null;
  priceUsd: number | null;
  priceUsdFoil: number | null;
  edhrecRank: number | null;
};

type CardResponse = {
  discover: { atlas: { cardRows: { nodes: CardDetailNode[] } } };
};

export function CardDetail({
  id,
  onBack,
  onSelect,
}: {
  id: string;
  onBack: () => void;
  onSelect?: (id: string) => void;
}) {
  const { data, loading, error } = useQuery<CardResponse>(CARD_QUERY, {
    variables: { id },
  });

  if (loading) return <p>Loading…</p>;
  if (error) return <p style={{ color: "#f77" }}>Error: {error.message}</p>;

  const card = data?.discover.atlas.cardRows.nodes[0];
  if (!card) return <p>Card not found.</p>;

  return (
    <>
      <button className="back" onClick={onBack}>← Back to list</button>
      <div className="detail">
        {card.imageUriLarge && <img src={card.imageUriLarge} alt={card.name} />}
        <div>
          <h2 style={{ marginTop: 0 }}>
            {card.name}{" "}
            {card.manaCost && (
              <span style={{ marginLeft: "0.5rem" }}>
                <ManaCost value={card.manaCost} />
              </span>
            )}
          </h2>
          <p style={{ color: "#9aa3bd", marginTop: 0 }}>{card.typeLine}</p>

          {card.oracleText && (
            <div className="oracle">
              {card.oracleText.split("\n").map((line, i) => (
                <p key={i} style={{ margin: "0 0 0.5em" }}>
                  <ManaCost value={line} />
                </p>
              ))}
            </div>
          )}

          {(card.power || card.toughness) && (
            <p><strong>P/T:</strong> {card.power}/{card.toughness}</p>
          )}
          {card.loyalty && <p><strong>Loyalty:</strong> {card.loyalty}</p>}
          {card.keywords.length > 0 && (
            <p><strong>Keywords:</strong> {card.keywords.join(", ")}</p>
          )}

          <hr style={{ borderColor: "#2a2f42" }} />
          <dl style={{ display: "grid", gridTemplateColumns: "max-content 1fr", gap: "0.25rem 1rem", margin: 0 }}>
            <dt>Set</dt><dd>{card.setName} ({card.set.toUpperCase()})</dd>
            <dt>Rarity</dt><dd>{card.rarity}</dd>
            <dt>Color identity</dt><dd>{card.colorIdentity.join("") || "colorless"}</dd>
            {card.artist && (<><dt>Artist</dt><dd>{card.artist}</dd></>)}
            {card.edhrecRank !== null && (<><dt>EDHREC rank</dt><dd>#{card.edhrecRank}</dd></>)}
            {card.priceUsd !== null && (<><dt>USD</dt><dd>${card.priceUsd.toFixed(2)}</dd></>)}
            {card.priceUsdFoil !== null && (<><dt>USD (foil)</dt><dd>${card.priceUsdFoil.toFixed(2)}</dd></>)}
          </dl>

          <p style={{ marginTop: "1rem" }}>
            <a href={card.scryfallUri} target="_blank" rel="noreferrer">View on Scryfall →</a>
          </p>

          {card.oracleId && <Rulings oracleId={card.oracleId} />}

          {onSelect && <NearestCards cardId={card.id} onSelect={onSelect} />}
        </div>
      </div>
    </>
  );
}
