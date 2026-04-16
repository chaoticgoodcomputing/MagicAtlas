import { ApolloClient, InMemoryCache } from "@apollo/client";

export const client = new ApolloClient({
  uri: "http://localhost:5250/trax/graphql",
  cache: new InMemoryCache(),
});
