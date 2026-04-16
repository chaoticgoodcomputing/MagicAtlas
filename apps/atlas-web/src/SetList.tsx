import { useMemo, useState } from "react";
import { useQuery } from "@apollo/client";
import { SETS_QUERY } from "./queries";

type SetNode = {
  id: string;
  code: string;
  name: string;
  setType: string;
  releasedAt: string | null;
  cardCount: number;
  iconSvgUri: string;
  scryfallUri: string;
  digital: boolean;
  parentSetCode: string | null;
};

type SetsResponse = {
  discover: { atlas: { setRows: { totalCount: number; nodes: SetNode[] } } };
};

export function SetList() {
  const [search, setSearch] = useState("");
  const [showDigital, setShowDigital] = useState(false);

  const { data, loading, error } = useQuery<SetsResponse>(SETS_QUERY);

  const filtered = useMemo(() => {
    const nodes = data?.discover.atlas.setRows.nodes ?? [];
    const q = search.trim().toLowerCase();
    return nodes.filter(
      (s) =>
        (showDigital || !s.digital) &&
        (q === "" || s.name.toLowerCase().includes(q) || s.code.toLowerCase().includes(q))
    );
  }, [data, search, showDigital]);

  return (
    <>
      <div className="filters">
        <input
          type="text"
          placeholder="Filter sets..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <label style={{ display: "flex", alignItems: "center", gap: "0.4rem", fontSize: "0.9rem" }}>
          <input
            type="checkbox"
            checked={showDigital}
            onChange={(e) => setShowDigital(e.target.checked)}
          />
          include digital-only
        </label>
        <span style={{ alignSelf: "center", color: "#9aa3bd" }}>
          {data ? `${filtered.length} of ${data.discover.atlas.setRows.totalCount}` : ""}
        </span>
      </div>

      {loading && <p>Loading…</p>}
      {error && <p style={{ color: "#f77" }}>Error: {error.message}</p>}

      <div className="set-grid">
        {filtered.map((s) => (
          <a
            key={s.id}
            href={s.scryfallUri}
            target="_blank"
            rel="noreferrer"
            className="set"
            title={`${s.setType} · released ${s.releasedAt ?? "—"}`}
          >
            <img src={s.iconSvgUri} alt={s.code} className="set-icon" />
            <div className="set-meta">
              <strong>{s.name}</strong>
              <span>
                {s.code.toUpperCase()} · {s.cardCount} cards
                {s.releasedAt && ` · ${s.releasedAt.slice(0, 4)}`}
              </span>
            </div>
          </a>
        ))}
      </div>
    </>
  );
}
