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

export const ATLAS_POINTS_QUERY = gql`
  query AtlasPoints {
    discover {
      atlas {
        atlasPointRows(first: 50000) {
          totalCount
          nodes { id x y textType }
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
