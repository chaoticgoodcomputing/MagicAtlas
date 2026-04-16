import { useQuery } from "@apollo/client";
import { RULINGS_QUERY } from "./queries";

type Ruling = {
  id: string;
  source: string;
  publishedAt: string;
  comment: string;
};

type RulingsResponse = {
  discover: { atlas: { rulingRows: { nodes: Ruling[] } } };
};

export function Rulings({ oracleId }: { oracleId: string }) {
  const { data, loading, error } = useQuery<RulingsResponse>(RULINGS_QUERY, {
    variables: { oracleId },
  });

  if (loading) return null;
  if (error) return <p style={{ color: "#f77" }}>Rulings: {error.message}</p>;

  const rulings = data?.discover.atlas.rulingRows.nodes ?? [];
  if (rulings.length === 0) return null;

  return (
    <section className="rulings">
      <h3>Rulings</h3>
      <ul>
        {rulings.map((r) => (
          <li key={r.id}>
            <div className="ruling-meta">
              <span>{new Date(r.publishedAt).toLocaleDateString()}</span>
              <span className="source">{r.source}</span>
            </div>
            <p>{r.comment}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}
