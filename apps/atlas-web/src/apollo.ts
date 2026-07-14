import { ApolloClient, InMemoryCache } from "@apollo/client";

export const client = new ApolloClient({
  uri: "http://localhost:55250/trax/graphql",
  cache: new InMemoryCache(),
});
