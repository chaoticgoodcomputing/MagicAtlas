import { ApolloClient, HttpLink, InMemoryCache } from "@apollo/client";

// The GraphQL API (atlas-api) listens on :55250 on the SAME host that serves this app. Derive its
// URL from the page's own hostname so it follows wherever the frontend is reached — localhost when
// local, the Tailscale name/IP when accessed over the tailnet — instead of a hardcoded localhost that
// a remote browser would resolve to itself. Override with VITE_API_URL for a split host/port.
const apiUrl =
  import.meta.env.VITE_API_URL ??
  `${window.location.protocol}//${window.location.hostname}:55250/trax/graphql`;

export const client = new ApolloClient({
  link: new HttpLink({ uri: apiUrl }),
  cache: new InMemoryCache(),
});
