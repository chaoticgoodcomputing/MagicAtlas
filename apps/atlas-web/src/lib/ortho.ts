// ─────────────────────────────────────────────────────────────────────────────
// Orthogonal (cardinal / right-angle) routing helpers — the transit-map look
// shared by the Metro map, Station focus and Synergy web.
//
// Ported from the concept canvas's `orthoPts` / `orthoPath` / `arrowMark`. Kept
// as pure functions (points → path string) so views can emit plain <path>/<g>
// SVG in JSX rather than reaching for d3's imperative append.
// ─────────────────────────────────────────────────────────────────────────────

export type Pt = [number, number];

/** An H-V-H or V-H-V elbow between two points; `off` shifts the mid-corridor so
 *  parallel lines fan out instead of overlapping. */
export function orthoPts(sx: number, sy: number, tx: number, ty: number, off = 0): Pt[] {
  const dx = tx - sx;
  const dy = ty - sy;
  if (Math.abs(dx) >= Math.abs(dy)) {
    const mx = (sx + tx) / 2 + off;
    return [[sx, sy], [mx, sy], [mx, ty], [tx, ty]];
  }
  const my = (sy + ty) / 2 + off;
  return [[sx, sy], [sx, my], [tx, my], [tx, ty]];
}

/** SVG path through `pts` with rounded 90° corners (radius clamped to half of
 *  each adjacent segment). */
export function orthoPath(pts: Pt[], R = 9): string {
  if (pts.length < 2) return "";
  if (pts.length === 2) return `M${pts[0][0]},${pts[0][1]} L${pts[1][0]},${pts[1][1]}`;

  let d = `M${pts[0][0]},${pts[0][1]}`;
  for (let i = 1; i < pts.length - 1; i++) {
    const [px, py] = pts[i - 1];
    const [cx, cy] = pts[i];
    const [nx, ny] = pts[i + 1];

    const inLen = Math.hypot(cx - px, cy - py);
    const outLen = Math.hypot(nx - cx, ny - cy);
    const r = Math.min(R, inLen / 2, outLen / 2);

    const inUx = (cx - px) / (inLen || 1);
    const inUy = (cy - py) / (inLen || 1);
    const outUx = (nx - cx) / (outLen || 1);
    const outUy = (ny - cy) / (outLen || 1);

    const p1x = cx - inUx * r;
    const p1y = cy - inUy * r;
    const p2x = cx + outUx * r;
    const p2y = cy + outUy * r;

    d += ` L${p1x},${p1y} Q${cx},${cy} ${p2x},${p2y}`;
  }
  const last = pts[pts.length - 1];
  d += ` L${last[0]},${last[1]}`;
  return d;
}

/** Angle (radians) of the final segment, for orienting an arrowhead. */
export function endAngle(pts: Pt[]): number {
  const a = pts[pts.length - 2];
  const b = pts[pts.length - 1];
  return Math.atan2(b[1] - a[1], b[0] - a[0]);
}

/** Point a fixed distance back from the end of a polyline (so the arrowhead
 *  sits off the target node rather than under it). */
export function pointBackFromEnd(pts: Pt[], back: number): Pt {
  const a = pts[pts.length - 2];
  const b = pts[pts.length - 1];
  const len = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1;
  const ux = (b[0] - a[0]) / len;
  const uy = (b[1] - a[1]) / len;
  return [b[0] - ux * back, b[1] - uy * back];
}

/** Point at parameter t (0..1) along the polyline by cumulative length. */
export function pointAt(pts: Pt[], t: number): { x: number; y: number; ang: number } {
  const segs: { a: Pt; b: Pt; len: number }[] = [];
  let total = 0;
  for (let i = 0; i < pts.length - 1; i++) {
    const len = Math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1]);
    segs.push({ a: pts[i], b: pts[i + 1], len });
    total += len;
  }
  let target = t * total;
  for (const s of segs) {
    if (target <= s.len || s === segs[segs.length - 1]) {
      const f = s.len ? target / s.len : 0;
      return {
        x: s.a[0] + (s.b[0] - s.a[0]) * f,
        y: s.a[1] + (s.b[1] - s.a[1]) * f,
        ang: Math.atan2(s.b[1] - s.a[1], s.b[0] - s.a[0]),
      };
    }
    target -= s.len;
  }
  const b = pts[pts.length - 1];
  return { x: b[0], y: b[1], ang: 0 };
}

/** The triangular arrowhead path (centered at origin, pointing +x). Translate +
 *  rotate it into place with the returned transform helper. */
export function arrowHeadPath(s = 5.2): string {
  return `M${-s},${-s} L${s * 1.4},0 L${-s},${s} Z`;
}

export const rotate = (x: number, y: number, angRad: number): string =>
  `translate(${x},${y}) rotate(${(angRad * 180) / Math.PI})`;
