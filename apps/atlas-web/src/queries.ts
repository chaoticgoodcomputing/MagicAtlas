import { gql } from "@apollo/client";

// Trax schema path for [TraxQueryModel(Namespace = "atlas")]:
//   discover.atlas.<fieldName>  where fieldName = camelCase plural of the entity class.

export const CARDS_QUERY = gql`
  query Cards(
    $first: Int = 30
    $after: String
    $where: CardRowFilterInput
    $order: [CardRowSortInput!]
  ) {
    discover {
      atlas {
        cardRows(first: $first, after: $after, where: $where, order: $order) {
          totalCount
          pageInfo { hasNextPage endCursor }
          nodes {
            id
            name
            manaCost
            typeLine
            rarity
            cmc
            colors
            imageUriNormal
            priceUsd
            setName
            edhrecRank
          }
        }
      }
    }
  }
`;

export const CARD_QUERY = gql`
  query Card($id: UUID!) {
    discover {
      atlas {
        cardRows(where: { id: { eq: $id } }, first: 1) {
          nodes {
            id
            name
            oracleId
            manaCost
            typeLine
            oracleText
            rarity
            cmc
            power
            toughness
            loyalty
            colors
            colorIdentity
            keywords
            imageUriLarge
            scryfallUri
            setName
            set
            artist
            priceUsd
            priceUsdFoil
            edhrecRank
          }
        }
      }
    }
  }
`;

export const RULINGS_QUERY = gql`
  query Rulings($oracleId: UUID!) {
    discover {
      atlas {
        rulingRows(
          where: { oracleId: { eq: $oracleId } }
          order: { publishedAt: ASC }
          first: 50
        ) {
          nodes { id source publishedAt comment }
        }
      }
    }
  }
`;

export const SETS_QUERY = gql`
  query Sets {
    discover {
      atlas {
        setRows(order: { releasedAt: DESC }, first: 500) {
          totalCount
          nodes {
            id
            code
            name
            setType
            releasedAt
            cardCount
            iconSvgUri
            scryfallUri
            digital
            parentSetCode
          }
        }
      }
    }
  }
`;

export const CARDS_BY_IDS_QUERY = gql`
  query CardsByIds($ids: [UUID!]!) {
    discover {
      atlas {
        cardRows(where: { id: { in: $ids } }, first: 200) {
          nodes {
            id
            name
            typeLine
            manaCost
            imageUriNormal
          }
        }
      }
    }
  }
`;

export const ATLAS_POINTS_QUERY = gql`
  query AtlasPoints {
    discover {
      atlas {
        atlasPointRows(first: 50000) {
          totalCount
          nodes { id cardId x y textType }
        }
      }
    }
  }
`;

export const SYMBOLS_QUERY = gql`
  query Symbols {
    discover {
      atlas {
        cardSymbolRows(first: 200) {
          nodes { symbol svgUri english }
        }
      }
    }
  }
`;

// ── Atlas foundation: resource families, edges, archetypes, ports ────────────
// These back the concept-explorer views through the hooks in data/atlas.ts.

export const FAMILY_GRAPH_QUERY = gql`
  query FamilyGraph {
    discover {
      atlas {
        resourceFamilyRows(first: 100) {
          totalCount
          nodes { family cards labels }
        }
        resourceEdgeRows(first: 500) {
          totalCount
          nodes { fromFamily toFamily realizingCombos bestTier engine origin }
        }
      }
    }
  }
`;

export const ARCHETYPES_QUERY = gql`
  query Archetypes {
    discover {
      atlas {
        archetypeRows(order: { realizingCombos: DESC }, first: 100) {
          totalCount
          nodes { signature families familyCount realizingCombos bestTier greenFraction exampleCards }
        }
      }
    }
  }
`;

export const HEADLINE_STATS_QUERY = gql`
  query HeadlineStats {
    discover {
      atlas {
        cardRows(first: 1) { totalCount }
        comboRows(first: 1) { totalCount }
        portRows(first: 1) { totalCount }
        resourceFamilyRows(first: 1) { totalCount }
        resourceEdgeRows(first: 1) { totalCount }
        archetypeRows(first: 1) { totalCount }
      }
    }
  }
`;

// Top cards for a single family (Station Focus rail). Distinct card names are
// derived client-side from the returned ports.
export const FAMILY_CARDS_QUERY = gql`
  query FamilyCards($family: String!) {
    discover {
      atlas {
        portRows(where: { family: { eq: $family } }, first: 200) {
          nodes { card family side }
        }
      }
    }
  }
`;

// Deck Lens: resolve a decklist (card names) to directional port coverage, the
// complete rings it already makes, and the near-miss closers one card away.
// Backed by the live discover.atlas.analyzeDeck resolver.
export const ANALYZE_DECK_QUERY = gql`
  query AnalyzeDeck($cards: [String!]!) {
    discover {
      atlas {
        analyzeDeck(cards: $cards) {
          coverage {
            family
            note
            emit { own subs { family count } }
            consume { own subs { family count } }
          }
          rings { cards ring tier pop confidence }
          nearMiss {
            missing
            ring
            resultTier
            cands { name evidence price score }
          }
        }
      }
    }
  }
`;

// Oracle port spans for one card (Card Explorer / Oracle showcase). The card's
// full newline-preserving oracle text plus every port's char-offset spans into
// it; data/atlas.ts reconstructs the highlighted segment list. `spans` is
// int[][] (each [start,end) an offset into oracleText) and is null until MAST's
// offsets are reseeded — the hook falls back to the hand-authored ORACLE map.
export const ORACLE_SPANS_QUERY = gql`
  query OracleSpans($card: String!) {
    discover {
      atlas {
        cardRows(where: { name: { eq: $card } }, first: 1) {
          nodes { oracleText typeLine }
        }
        portRows(where: { card: { eq: $card } }, first: 50) {
          nodes { family side oracleLineIndex spans }
        }
      }
    }
  }
`;

// ── Card profile page (views/CardPage.tsx) ──────────────────────────────────
// One card's full record by name (id/oracleId/type/mana/oracle/imagery/price/
// meta) plus its live ports. `portRows.confidence` is a nullable Float — surface
// it only for Inferred ports. `spans` is omitted here (the Oracle highlighting
// path uses ORACLE_SPANS_QUERY / useOracle instead).
export const CARD_PROFILE_QUERY = gql`
  query CardProfile($name: String!) {
    discover {
      atlas {
        cardRows(where: { name: { eq: $name } }, first: 1) {
          nodes {
            id
            oracleId
            name
            typeLine
            manaCost
            oracleText
            imageUriNormal
            imageUriLarge
            priceUsd
            edhrecRank
            scryfallUri
            colors
            keywords
          }
        }
        portRows(where: { card: { eq: $name } }, first: 50) {
          nodes { family side tier confidence label }
        }
      }
    }
  }
`;

// Named combos this card appears in. `cards` is a " + "-joined string; the
// filter is a substring `contains`, so callers re-check the exact name after
// splitting. Ordered by popularity, capped for the panel.
export const CARD_COMBOS_QUERY = gql`
  query CardCombos($name: String!) {
    discover {
      atlas {
        comboRows(
          where: { cards: { contains: $name } }
          order: { popularity: DESC }
          first: 30
        ) {
          totalCount
          nodes { comboId cards familyRing tier popularity }
        }
      }
    }
  }
`;

// Anchor profile for a card (only present when the card is a blocker): how many
// combos it blocks / is the sole blocker for, plus its co-stars.
export const CARD_ANCHOR_QUERY = gql`
  query CardAnchor($name: String!) {
    discover {
      atlas {
        comboAnchorRows(where: { card: { eq: $name } }, first: 1) {
          nodes {
            card
            blockedComboCount
            soleBlockerCount
            popularityMass
            maxComboPopularity
            coStars { card sharedCombos sharedPopularity alsoUnparsed }
          }
        }
      }
    }
  }
`;

// Candidate ports on one side of a family set (Card Explorer emitter/consumer
// columns). Family set already expanded to include super/subgroups.
// `tier` is the port's fidelity tier (Green/Amber/Inferred/Declared); it is
// nullable and stays null until the pipeline reseeds the backfilled tiers — the
// hook falls back to a neutral tier so the UI never shows a blank one. The
// PortRow schema exposes no `confidence` field, so none is selected here.
export const PORT_CANDIDATES_QUERY = gql`
  query PortCandidates($families: [String!]!, $side: String!) {
    discover {
      atlas {
        portRows(where: { side: { eq: $side }, family: { in: $families } }, first: 200) {
          nodes { card family side tier }
        }
      }
    }
  }
`;
