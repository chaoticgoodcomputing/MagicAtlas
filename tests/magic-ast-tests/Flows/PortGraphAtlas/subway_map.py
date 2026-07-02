"""The resource "subway map" — the family-collapsed port graph as a node-link diagram.

Stations are the ~17 canonical resource families (mana, token, sacrifice, dice, …), sized by card
"ridership" (how many in-scope cards project into the family). Lines are the directed family→family
transitions: an ARM line is the rules/physics (an emit feeding a cost), a WIRING line is a card's own
text (a cost/trigger driving an effect). The 16 fundamental two-family ENGINES — bidirectional pairs
like blink↔etb, death↔sacrifice, mana↔untap — are drawn as bold highlighted loops; they're the atoms
every larger combo archetype is built from.

Inputs (positional, from the C# FamilyNodeRow / FamilyEdgeRow catalog items):
    nodes: DataFrame [family, cards, labels]
    edges: DataFrame [from, to, armweight, wiringweight, engine]
Output:
    str — a standalone Plotly HTML document.
"""
from __future__ import annotations

import logging
import math

import networkx as nx
import pandas as pd
import plotly.graph_objects as go
from flowthru import step

logger = logging.getLogger(__name__)

# A subway-line palette — one distinct hue per family so the stations read as a colour-coded map.
_FAMILY_COLORS = {
    "mana": "#e7ba52",       # gold — the economy
    "tap": "#bab0ac",        # grey
    "untap": "#8cd17d",      # light green
    "token": "#59a14f",      # green
    "sacrifice": "#4c78a8",  # blue
    "death": "#b279a2",      # purple
    "etb": "#9c6b4f",        # brown
    "recur": "#d37295",      # pink
    "cast": "#76b7b2",       # cyan
    "copy": "#72b7b2",       # teal
    "damage": "#e15759",     # red
    "dice": "#ff9d9a",       # salmon
    "life": "#f28e2b",       # orange
    "blink": "#5b9bd5",      # sky
    "combat": "#af7aa1",     # mauve
    "counter": "#9d7660",    # tan
    "phase": "#bab0ac",      # grey
}
_DEFAULT_COLOR = "#9d755d"
_ENGINE_COLOR = "#c1272d"   # bold crimson — the fundamental two-family loops
_ONEWAY_COLOR = "#c8c8c8"   # light grey — a one-way transition


def _arrow_trace(segments, color, size=10, along=0.66):
    """Directional arrowheads: a triangle `along` the way down each edge, rotated toward the target."""
    xs, ys, angles = [], [], []
    for (x0, y0), (x1, y1) in segments:
        dx, dy = x1 - x0, y1 - y0
        xs.append(x0 + along * dx)
        ys.append(y0 + along * dy)
        angles.append(math.degrees(math.atan2(dx, dy)))  # plotly marker angle: CW from north
    return go.Scatter(
        x=xs, y=ys, mode="markers", hoverinfo="none", showlegend=False,
        marker=dict(symbol="triangle-up", size=size, color=color, angle=angles, line=dict(width=0)),
    )


def _line_trace(segments, color, width, name, legend):
    xs, ys = [], []
    for (x0, y0), (x1, y1) in segments:
        xs += [x0, x1, None]
        ys += [y0, y1, None]
    return go.Scatter(
        x=xs, y=ys, mode="lines", line=dict(width=width, color=color),
        name=name, showlegend=legend, hoverinfo="none",
    )


def _build_subway_map(nodes: pd.DataFrame, edges: pd.DataFrame) -> str:
    nodes = nodes.copy()
    edges = edges.copy()
    nodes.columns = [c.lower() for c in nodes.columns]
    edges.columns = [c.lower() for c in edges.columns]

    g = nx.DiGraph()
    for _, r in nodes.iterrows():
        g.add_node(r["family"], cards=int(r["cards"]), labels=int(r["labels"]))
    for _, r in edges.iterrows():
        g.add_edge(r["from"], r["to"], engine=bool(r["engine"]),
                   arm=int(r["armweight"]), wiring=int(r["wiringweight"]))
    logger.info("[subway_map] %d stations, %d lines (%d engine)",
                g.number_of_nodes(), g.number_of_edges(),
                sum(1 for _, _, d in g.edges(data=True) if d["engine"]))

    # spring_layout is pure-networkx (numpy only — no scipy dep); the seed makes it deterministic. k
    # spaces the ~17 stations out so the labels don't collide.
    pos = nx.spring_layout(g, seed=7, k=1.4 / math.sqrt(max(1, g.number_of_nodes())), iterations=200)

    engine_segs, oneway_segs = [], []
    for u, v, d in g.edges(data=True):
        (engine_segs if d["engine"] else oneway_segs).append((pos[u], pos[v]))

    fig = go.Figure()
    # One-way transitions first (light, underneath), then the bold engine loops on top.
    fig.add_trace(_line_trace(oneway_segs, _ONEWAY_COLOR, 1.1, "one-way transition", True))
    fig.add_trace(_arrow_trace(oneway_segs, _ONEWAY_COLOR, size=8))
    fig.add_trace(_line_trace(engine_segs, _ENGINE_COLOR, 2.6, "fundamental engine (loop)", True))
    fig.add_trace(_arrow_trace(engine_segs, _ENGINE_COLOR, size=11))

    # Stations. Size ∝ sqrt(card mass) so the economy hubs read big without swamping the map.
    max_cards = max((g.nodes[n]["cards"] for n in g.nodes), default=1) or 1
    xs, ys, sizes, colors, texts, hovers = [], [], [], [], [], []
    for n in g.nodes:
        c = g.nodes[n]["cards"]
        deg_in, deg_out = g.in_degree(n), g.out_degree(n)
        xs.append(pos[n][0])
        ys.append(pos[n][1])
        sizes.append(20 + 46 * math.sqrt(c / max_cards))
        colors.append(_FAMILY_COLORS.get(n, _DEFAULT_COLOR))
        texts.append(n)
        hovers.append(
            f"<b>{n}</b><br>{c} cards · {g.nodes[n]['labels']} labels"
            f"<br>in {deg_in} · out {deg_out}"
        )
    fig.add_trace(go.Scatter(
        x=xs, y=ys, mode="markers+text", text=texts, textposition="middle center",
        textfont=dict(size=10, color="#111"), hovertext=hovers, hoverinfo="text",
        marker=dict(size=sizes, color=colors, line=dict(width=1.5, color="#2b2b2b")),
        showlegend=False,
    ))

    n_engine = sum(1 for _, _, d in g.edges(data=True) if d["engine"]) // 2
    fig.update_layout(
        title=(
            "MAST — the resource subway map "
            f"({g.number_of_nodes()} families · {n_engine} fundamental two-family engines)"
        ),
        template="plotly_white",
        legend=dict(orientation="h", yanchor="bottom", y=1.02, xanchor="left", x=0),
        height=760, margin=dict(l=20, r=20, t=70, b=20),
        annotations=[dict(
            text="station size ∝ cards · bold crimson = bidirectional engine (an infinite loop) · grey = one-way",
            xref="paper", yref="paper", x=0, y=-0.04, showarrow=False,
            font=dict(size=11, color="#666"), xanchor="left",
        )],
    )
    for axis in ("xaxis", "yaxis"):
        fig.layout[axis].update(showgrid=False, zeroline=False, showticklabels=False)
    return fig.to_html(full_html=True, include_plotlyjs="cdn")


@step(inputs=["FamilyNodeRow", "FamilyEdgeRow"], outputs="FamilyGraphHtml", cacheable=True)
def subway_map(nodes: pd.DataFrame, edges: pd.DataFrame) -> str:
    return _build_subway_map(nodes, edges)
