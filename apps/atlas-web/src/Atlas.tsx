import { useQuery } from "@apollo/client";
import { useEffect, useMemo, useRef, useState } from "react";
import createScatterplot from "regl-scatterplot";
import { ATLAS_POINTS_QUERY } from "./queries";
import { CardDetail } from "./CardDetail";

type Point = { id: string; x: number; y: number; textType: string };

type AtlasPointsResponse = {
  discover: { atlas: { atlasPointRows: { totalCount: number; nodes: Point[] } } };
};

// regl-scatterplot expects points as [x, y, categoryIndex] tuples, normalised to [-1, 1].
// Colors are indexed by categoryIndex.
const TEXT_TYPES = ["oracle", "keyword", "triggered", "activated", "passive"] as const;
const COLORS: [number, number, number, number][] = [
  [0.48, 0.69, 1.0, 0.85], // oracle — blue
  [0.60, 0.40, 0.90, 0.85], // keyword — purple
  [1.00, 0.64, 0.30, 0.85], // triggered — orange
  [0.95, 0.30, 0.30, 0.85], // activated — red
  [0.30, 0.80, 0.55, 0.85], // passive — green
];

function normalize(points: Point[]): [number, number, number][] {
  if (points.length === 0) return [];
  let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
  for (const p of points) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
    if (p.y < minY) minY = p.y;
    if (p.y > maxY) maxY = p.y;
  }
  const spanX = maxX - minX || 1;
  const spanY = maxY - minY || 1;
  // Use the larger span for both axes so aspect ratio is preserved.
  const span = Math.max(spanX, spanY);
  const cx = (minX + maxX) / 2;
  const cy = (minY + maxY) / 2;
  return points.map((p) => {
    const cat = Math.max(0, TEXT_TYPES.indexOf(p.textType as typeof TEXT_TYPES[number]));
    return [
      ((p.x - cx) / span) * 1.8,
      ((p.y - cy) / span) * 1.8,
      cat,
    ];
  });
}

export function Atlas() {
  const { data, loading, error } = useQuery<AtlasPointsResponse>(ATLAS_POINTS_QUERY);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const scatterplotRef = useRef<ReturnType<typeof createScatterplot> | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const points = data?.discover.atlas.atlasPointRows.nodes ?? [];
  const pointsData = useMemo(() => normalize(points), [points]);

  useEffect(() => {
    if (!canvasRef.current || pointsData.length === 0) return;

    const canvas = canvasRef.current;
    const { width, height } = canvas.getBoundingClientRect();
    canvas.width = width * window.devicePixelRatio;
    canvas.height = height * window.devicePixelRatio;

    const scatterplot = createScatterplot({
      canvas,
      width,
      height,
      pointSize: 3,
      pointColor: COLORS,
      opacity: 0.85,
      lassoColor: [1, 1, 1, 0.2],
    });
    scatterplotRef.current = scatterplot;

    scatterplot.draw(pointsData);

    scatterplot.subscribe("pointOver", (pointIndex: number) => {
      canvas.style.cursor = "pointer";
      const p = points[pointIndex];
      if (p) canvas.title = p.id;
    });
    scatterplot.subscribe("pointOut", () => {
      canvas.style.cursor = "default";
      canvas.title = "";
    });
    scatterplot.subscribe("select", ({ points: indices }: { points: number[] }) => {
      if (indices.length > 0) {
        const p = points[indices[0]];
        if (p) setSelectedId(p.id);
      }
    });

    return () => {
      scatterplot.destroy();
      scatterplotRef.current = null;
    };
  }, [pointsData, points]);

  if (loading) return <p>Loading atlas ({points.length || "…"} points)…</p>;
  if (error) return <p style={{ color: "#f77" }}>Error: {error.message}</p>;
  if (points.length === 0) {
    return (
      <div className="empty-atlas">
        <p><strong>No atlas points yet.</strong></p>
        <p style={{ color: "#9aa3bd" }}>
          Run the embedding pipeline to generate <code>dumps/atlas-points.json</code>, then restart the API:
        </p>
        <pre>dotnet run --project apps/atlas -- run OracleEmbedding</pre>
      </div>
    );
  }

  return (
    <div className="atlas-container">
      <div className="atlas-meta">
        <span>{points.length.toLocaleString()} cards · scroll to zoom · drag to pan · click a point</span>
        <div className="legend">
          {TEXT_TYPES.map((t, i) => (
            <span key={t} className="legend-item">
              <span
                className="legend-swatch"
                style={{
                  background: `rgba(${Math.round(COLORS[i][0] * 255)}, ${Math.round(COLORS[i][1] * 255)}, ${Math.round(COLORS[i][2] * 255)}, 0.9)`,
                }}
              />
              {t}
            </span>
          ))}
        </div>
      </div>
      <canvas ref={canvasRef} className="atlas-canvas" />

      {selectedId && (
        <aside className="drawer" onClick={(e) => e.target === e.currentTarget && setSelectedId(null)}>
          <div className="drawer-panel">
            <CardDetail id={selectedId} onBack={() => setSelectedId(null)} />
          </div>
        </aside>
      )}
    </div>
  );
}
