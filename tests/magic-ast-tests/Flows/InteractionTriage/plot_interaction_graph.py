"""Interaction-graph visualization — two network subplots on one Plotly figure.

Left subplot  — the abstract LABEL-level grammar: port label -> port label
                (sac-outlet, death-payoff, token-doubler). Nodes colored by label.
Right subplot — the CARD-level CYCLES: every reconstructed ATOMIC loop (length-3
                sac->death->doubler) rendered as its OWN isolated cycle (ports
                duplicated per cycle so shared hub cards don't merge), tiled in a
                grid, GREEN (confirmed) loops first. Node color = port label
                (matching the left); edge color = certainty tier; arrowheads show
                direction; hovering a node shows the card's oracle text.

Inputs arrive as pandas DataFrames marshalled from the C# LabelEdgeRow /
CardEdgeRow / PortNodeRow catalog items. Cycles are found over the UNION graph
(ports shared across combos), bounded to length 3 so we surface fundamental
loops rather than their combinatorial multi-doubler compositions.
"""

import html
import logging
import math

import networkx as nx
import pandas as pd
import plotly.graph_objects as go
from flowthru import step
from plotly.subplots import make_subplots

logger = logging.getLogger(__name__)

# Port-label palette — avoids the tier greens/ambers/reds so node color (role)
# and edge color (certainty) never collide.
_LABEL_COLORS = {
    "sac-outlet": "#4c78a8",     # blue
    "death-payoff": "#b279a2",   # purple
    "token-doubler": "#439894",  # teal
}
_DEFAULT_LABEL_COLOR = "#9d755d"
_TIER_COLOR = {"Green": "#2ca02c", "Amber": "#e6a817", "Red": "#d62728"}
_TIER_ORDER = ["Green", "Amber", "Red"]


def _label_of(node: str) -> str:
    return node.rsplit("::", 1)[-1]


def _card_of(node: str) -> str:
    return node.rsplit("::", 1)[0]


def _oracle_html(card: str, oracle: dict) -> str:
    text = oracle.get(card, "")
    return html.escape(text).replace("\n", "<br>") if text else ""


def _hover(node: str, oracle: dict) -> str:
    if "::" not in node:  # left-chart label node
        return node
    card, label = _card_of(node), _label_of(node)
    body = _oracle_html(card, oracle)
    return f"<b>{html.escape(card)}</b> · [{label}]" + (f"<br>{body}" if body else "")


def _node_traces(nodes, pos, oracle, *, size, font, show_text, legend, seen_labels):
    """One marker trace per label (so the legend reads as a label key). `nodes` is
    a list of (key, display_node); `pos` maps key -> (x, y)."""
    by_label: dict[str, list] = {}
    for key, node in nodes:
        by_label.setdefault(_label_of(node), []).append((key, node))
    traces = []
    for label, items in sorted(by_label.items()):
        color = _LABEL_COLORS.get(label, _DEFAULT_LABEL_COLOR)
        show = legend and label not in seen_labels
        if show:
            seen_labels.add(label)
        traces.append(
            go.Scatter(
                x=[pos[k][0] for k, _ in items],
                y=[pos[k][1] for k, _ in items],
                mode="markers+text" if show_text else "markers",
                text=[_label_of(n) if show_text else "" for _, n in items],
                textposition="top center",
                textfont=dict(size=font),
                hovertext=[_hover(n, oracle) for _, n in items],
                hoverinfo="text",
                marker=dict(size=size, color=color, line=dict(width=1, color="#2b2b2b")),
                name=label,
                legendgroup=f"label:{label}",
                showlegend=show,
            )
        )
    return traces


def _line_trace(segments, color, *, name=None, legend=False):
    xs: list = []
    ys: list = []
    for (x0, y0), (x1, y1) in segments:
        xs += [x0, x1, None]
        ys += [y0, y1, None]
    return go.Scatter(
        x=xs, y=ys, mode="lines", line=dict(width=1.3, color=color),
        name=name or "edge", showlegend=legend, hoverinfo="none",
    )


def _arrow_trace(segments, color):
    """Directional arrowheads: a triangle marker ~62% along each edge, rotated to point at the target."""
    xs: list = []
    ys: list = []
    angles: list = []
    for (x0, y0), (x1, y1) in segments:
        dx, dy = x1 - x0, y1 - y0
        xs.append(x0 + 0.62 * dx)
        ys.append(y0 + 0.62 * dy)
        angles.append(math.degrees(math.atan2(dx, dy)))  # plotly angle: CW from north
    return go.Scatter(
        x=xs, y=ys, mode="markers",
        marker=dict(symbol="triangle-up", size=9, color=color, angle=angles, line=dict(width=0)),
        hoverinfo="none", showlegend=False,
    )


def _find_cycles(card_edges: pd.DataFrame, length_bound: int = 3, cap: int = 120):
    """ATOMIC elementary cycles over the whole UNION graph, bounded to `length_bound` so we surface
    fundamental sac->death->doubler loops rather than their combinatorial compositions. Deduped by
    node set, ranked GREEN-first then shortest, display-capped. Returns (cycles, total_found)."""
    g = nx.DiGraph()
    for _, r in card_edges.iterrows():
        g.add_edge(
            f'{r["fromcard"]}::{r["fromlabel"]}',
            f'{r["tocard"]}::{r["tolabel"]}',
            tier=r["tier"],
        )

    cycles = []
    seen: set[frozenset] = set()
    for cyc in nx.simple_cycles(g, length_bound=length_bound):
        if len(cyc) < 2:
            continue
        key = frozenset(cyc)
        if key in seen:
            continue
        seen.add(key)
        edges = [
            (cyc[i], cyc[(i + 1) % len(cyc)], g.edges[cyc[i], cyc[(i + 1) % len(cyc)]]["tier"])
            for i in range(len(cyc))
        ]
        cycles.append((cyc, edges))

    cycles.sort(key=lambda ce: (0 if all(t == "Green" for _, _, t in ce[1]) else 1, len(ce[0])))
    return cycles[:cap], len(cycles)


@step(inputs=["LabelEdgeRow", "CardEdgeRow", "PortNodeRow"], outputs="InteractionGraphHtml")
def plot_interaction_graph(
    label_edges: pd.DataFrame, card_edges: pd.DataFrame, port_nodes: pd.DataFrame
) -> str:
    for df in (label_edges, card_edges, port_nodes):
        df.columns = [c.lower() for c in df.columns]
    oracle = {r["card"]: r["oracletext"] for _, r in port_nodes.iterrows()}

    cycles, total = _find_cycles(card_edges) if len(card_edges) else ([], 0)
    logger.info(
        "[plot_interaction_graph] %d label edges, %d union card edges, %d nodes -> %d atomic cycles (showing %d)",
        len(label_edges), len(card_edges), len(port_nodes), total, len(cycles),
    )

    fig = make_subplots(
        rows=1, cols=2, column_widths=[0.32, 0.68],
        subplot_titles=(
            "Label grammar",
            f"Atomic cycles — showing {len(cycles)} of {total} (sac→death→doubler, GREEN-first)",
        ),
        horizontal_spacing=0.05,
    )

    seen_labels: set[str] = set()

    # ---- left: label grammar ----
    lg = nx.DiGraph()
    for _, r in label_edges.iterrows():
        lg.add_edge(r["from"], r["to"])
    if lg.number_of_nodes():
        lpos = nx.spring_layout(lg, seed=42)
        lsegs = [(lpos[u], lpos[v]) for u, v in lg.edges()]
        fig.add_trace(_line_trace(lsegs, "#bbb"), row=1, col=1)
        fig.add_trace(_arrow_trace(lsegs, "#999"), row=1, col=1)
        for t in _node_traces(
            [(n, n) for n in lg.nodes()], lpos, oracle,
            size=30, font=11, show_text=True, legend=True, seen_labels=seen_labels,
        ):
            fig.add_trace(t, row=1, col=1)

    # ---- right: atomic cycles, ports duplicated per cycle, tiled ----
    if cycles:
        cols = max(1, math.ceil(math.sqrt(len(cycles))))
        cell, radius = 3.0, 1.0
        pos: dict = {}
        node_items: list = []
        segs_by_tier: dict[str, list] = {}
        for idx, (cyc, edges) in enumerate(cycles):
            cx, cy = (idx % cols) * cell, -(idx // cols) * cell
            k = len(cyc)
            for j, node in enumerate(cyc):
                ang = 2 * math.pi * j / k
                pos[(idx, node)] = (cx + radius * math.cos(ang), cy + radius * math.sin(ang))
                node_items.append(((idx, node), node))
            for u, v, tier in edges:
                segs_by_tier.setdefault(tier, []).append((pos[(idx, u)], pos[(idx, v)]))

        seen_tiers: set[str] = set()
        for tier in _TIER_ORDER:
            segs = segs_by_tier.get(tier)
            if not segs:
                continue
            show = tier not in seen_tiers
            seen_tiers.add(tier)
            fig.add_trace(
                _line_trace(segs, _TIER_COLOR[tier], name=f"{tier} edge", legend=show), row=1, col=2
            )
            fig.add_trace(_arrow_trace(segs, _TIER_COLOR[tier]), row=1, col=2)
        for t in _node_traces(
            node_items, pos, oracle,
            size=11, font=7, show_text=False, legend=True, seen_labels=seen_labels,
        ):
            fig.add_trace(t, row=1, col=2)
    else:
        fig.add_annotation(text="no reconstructed cycles", xref="x2", yref="y2", showarrow=False)

    for axis in ("xaxis", "yaxis", "xaxis2", "yaxis2"):
        fig.layout[axis].update(showgrid=False, zeroline=False, showticklabels=False)

    fig.update_layout(
        title="MAST interaction graph — label grammar vs reconstructed atomic cycles",
        template="plotly_white",
        legend=dict(title="node = port label · edge = tier", orientation="v", x=1.01, y=1),
        height=820,
        margin=dict(l=20, r=20, t=70, b=20),
    )

    return fig.to_html(full_html=True, include_plotlyjs="cdn")
