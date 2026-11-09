"""Mermaid text for flowcharts and maps.

Output rules (diff-friendly and safe on GitHub):
- node ids are content-based (see flow.py), never 1, 2, 3 ...
- every class sets fill, stroke AND text color, so diagrams read the same in light and dark mode
- classic flowchart syntax only (no v11 shape syntax), so older renderers (GitHub) draw it too
- the front-matter config (wider wrapping, monospace) is optional: renderers that ignore it still draw the chart
"""
from __future__ import annotations

import re

from .flow import Flow, FNode

WRAP = 48
MAX_LINES_PER_STMT = 3

FLOW_CONFIG = """---
config:
  fontFamily: monospace
  flowchart:
    wrappingWidth: 520
    nodeSpacing: 28
    rankSpacing: 34
---"""
MAP_CONFIG = """---
config:
  flowchart:
    wrappingWidth: 320
    nodeSpacing: 18
    rankSpacing: 60
---"""

UNITY_WHEN = {
    "Awake": "when created", "OnEnable": "when enabled", "Start": "before its first frame",
    "Update": "every frame", "LateUpdate": "every frame, after all Updates", "FixedUpdate": "every physics step",
    "OnTriggerEnter2D": "something enters its trigger", "OnTriggerStay2D": "every physics step while touching",
    "OnTriggerExit2D": "something leaves its trigger", "OnCollisionEnter2D": "a collision starts",
    "OnCollisionStay2D": "every physics step while colliding", "OnCollisionExit2D": "a collision ends",
    "OnDisable": "when disabled", "OnDestroy": "when destroyed", "OnValidate": "Editor: a value changed in the Inspector",
    "OnDrawGizmos": "Editor: draws gizmos", "OnDrawGizmosSelected": "Editor: draws gizmos when selected",
    "OnApplicationQuit": "when the game quits", "OnGUI": "every GUI event",
}
UNITY_PHASE = {
    "Awake": "start-up", "OnEnable": "start-up", "Start": "start-up",
    "Update": "every frame", "LateUpdate": "every frame",
    "FixedUpdate": "physics", "OnTriggerEnter2D": "physics", "OnTriggerStay2D": "physics", "OnTriggerExit2D": "physics",
    "OnCollisionEnter2D": "physics", "OnCollisionStay2D": "physics", "OnCollisionExit2D": "physics",
    "OnTriggerEnter": "physics", "OnTriggerStay": "physics", "OnTriggerExit": "physics",
    "OnDisable": "teardown", "OnDestroy": "teardown", "OnApplicationQuit": "teardown",
    "OnValidate": "editor", "OnDrawGizmos": "editor", "OnDrawGizmosSelected": "editor",
}
PHASE_ORDER = ["start-up", "every frame", "physics", "teardown", "editor"]

# --- palette: structure (browse) -------------------------------------------------------
STYLE_BROWSE = """\
classDef unity fill:#fff1c2,stroke:#9a6700,stroke-width:2px,color:#3b2300
classDef start fill:#ddf4ff,stroke:#0969da,stroke-width:2px,color:#0a3069
classDef step fill:#ffffff,stroke:#8c959f,color:#1f2328
classDef calls fill:#ffffff,stroke:#0969da,stroke-width:2px,color:#0a3069
classDef guard fill:#ffffff,stroke:#6e7781,color:#1f2328
classDef cond fill:#f6f8fa,stroke:#6e7781,color:#1f2328
classDef loop fill:#fbefff,stroke:#8250df,color:#3e1f79
classDef exit fill:#eaeef2,stroke:#57606a,color:#24292f
classDef wait fill:#fff8f0,stroke:#bc4c00,color:#4d1d00
classDef fin fill:#eaeef2,stroke:#8c959f,color:#57606a
classDef join fill:#d0d7de,stroke:#8c959f,color:#57606a"""

# --- palette: diff. Okabe-Ito blue / vermilion / yellow (safe for red-green color blindness), and color is
# never the only cue: + / − / ~ markers, thick border for added, dashed border for removed, gray for unchanged
STYLE_DIFF = """\
classDef same fill:#f6f8fa,stroke:#d0d7de,color:#6e7781
classDef added fill:#dcecf7,stroke:#0072b2,stroke-width:3px,color:#00304d
classDef removed fill:#fbe4d8,stroke:#d55e00,stroke-width:2px,stroke-dasharray:6 4,color:#5c2600
classDef modified fill:#fbf5c4,stroke:#8a7a00,stroke-width:3px,color:#3d3500
classDef impacted fill:#ffffff,stroke:#6e7781,stroke-width:2px,stroke-dasharray:2 3,color:#24292f"""

EDGE_ADDED = "stroke:#0072b2,stroke-width:3px"
EDGE_REMOVED = "stroke:#d55e00,stroke-width:2px,stroke-dasharray:6 4"

MARK = {"added": "+ ", "removed": "− ", "modified": "~ ", "same": ""}


def esc(text: str) -> str:
    """Text safe inside a quoted Mermaid label (entity codes, so nothing is read as HTML or syntax)."""
    text = text.replace("#", "#35;")
    return (text.replace("&", "#38;").replace('"', "#34;").replace("<", "#60;").replace(">", "#62;")
            .replace("`", "#96;").replace("|", "#124;"))


def wrap(text: str, width: int = WRAP, max_lines: int = MAX_LINES_PER_STMT) -> list:
    """Break long code at spaces / after commas and dots, never inside a word."""
    if len(text) <= width:
        return [text]
    tokens = re.findall(r"[^ ,.(]+[ ,.(]?|[ ,.(]", text)
    lines, cur = [], ""
    for tok in tokens:
        if len(cur) + len(tok) > width and cur.strip():
            lines.append(cur.rstrip())
            cur = "  " + tok.lstrip()
        else:
            cur += tok
    if cur.strip():
        lines.append(cur.rstrip())
    if len(lines) > max_lines:
        lines = lines[:max_lines]
        lines[-1] = lines[-1][: width - 1] + " …"
    return lines


# ----------------------------------------------------------------------------- flowcharts

def _label(node: FNode, diff: bool, comments: bool, subtitle: str = "") -> str:
    out = []
    for i, ln in enumerate(node.lines):
        mark = MARK[ln.status] if diff else ""
        text = ln.text
        if node.kind in ("decision", "case") and not text.startswith("switch"):
            text = text + " ?"
        if node.kind == "loop":
            text = "↻ " + text
        if node.kind == "wait":
            text = "⏳ " + text
        for j, p in enumerate(wrap(text)):
            out.append(esc((mark if j == 0 else ("  " if mark else "")) + p))
        if diff and ln.status == "modified" and ln.old:
            old = ln.old if len(ln.old) <= WRAP else ln.old[: WRAP - 2] + " …"
            out.append("<i>was: " + esc(old) + "</i>")
        if comments and ln.comment and (i == 0 or ln.comment != node.lines[i - 1].comment):
            out.append("<i>// " + esc(wrap(ln.comment, WRAP + 4, 1)[0]) + "</i>")
    if subtitle:
        out.append("<i>" + esc(subtitle) + "</i>")
    return "<br>".join(out)


def _shape(node: FNode, label: str) -> str:
    k = node.kind
    q = f'"{label}"'
    if k in ("start", "end", "exit"):
        return f"([{q}])"
    if k in ("decision", "switch", "try", "case"):
        return "{{" + q + "}}"
    if k == "loop":  # rounded boxes get little padding: pad with no-break spaces so the label isn't clipped
        return f'("\u00a0\u00a0{label}\u00a0\u00a0")'
    if k == "guard":
        return f">{q}]"
    if k == "wait":
        return f"[/{q}/]"
    if k == "join":  # a merge point on a switch rail
        return '((" "))'
    if k in ("block", "ghost") and node.calls:
        return f"[[{q}]]"
    return f"[{q}]"


def _browse_class(node: FNode, unity: bool) -> str:
    k = node.kind
    if k == "start":
        return "unity" if unity else "start"
    if k == "end":
        return "fin"
    if k in ("decision", "switch", "try", "case"):
        return "cond"
    if k == "loop":
        return "loop"
    if k == "exit":
        return "exit"
    if k == "wait":
        return "wait"
    if k == "join":
        return "join"
    if node.calls:
        return "calls"
    if k == "guard":
        return "guard"
    return "step"


def flow_to_mermaid(flow: Flow, *, unity_event: str | None = None, coroutine: bool = False, diff: bool = False,
                    comments: bool = True, config: bool = True) -> str:
    subtitle = ""
    if unity_event:
        subtitle = "Unity: " + UNITY_WHEN.get(unity_event, "engine callback")
    elif coroutine:
        subtitle = "coroutine: can wait between frames"
    lines = ([FLOW_CONFIG] if config else []) + ["flowchart TD"]
    # exits last: dagre then tends to place them right of the main path (DRAKON: the main path goes straight down)
    for n in [x for x in flow.nodes if x.kind != "exit"] + [x for x in flow.nodes if x.kind == "exit"]:
        lines.append(f"  {n.id}{_shape(n, _label(n, diff, comments, subtitle if n.id == 'nS' else ''))}")
    link_styles = []
    for i, e in enumerate(flow.edges):
        arrow = {"back": "-.->", "invisible": "~~~"}.get(e.kind, "-->")
        lab = f"|{esc(e.label)}|" if e.label and e.kind != "invisible" else ""
        lines.append(f"  {e.src} {arrow}{lab} {e.dst}")
        if diff and e.status == "added":
            link_styles.append(f"  linkStyle {i} {EDGE_ADDED}")
        elif diff and e.status == "removed":
            link_styles.append(f"  linkStyle {i} {EDGE_REMOVED}")
    lines += link_styles
    lines += ["  " + l for l in (STYLE_DIFF if diff else STYLE_BROWSE).splitlines()]
    groups: dict[str, list] = {}
    for n in flow.nodes:
        cls = n.status if diff else _browse_class(n, bool(unity_event))
        groups.setdefault(cls, []).append(n.id)
    for cls in sorted(groups):
        lines.append(f"  class {','.join(groups[cls])} {cls}")
    return "\n".join(lines)


# ----------------------------------------------------------------------------- maps

MAP_STYLE = """\
classDef engine fill:#fff1c2,stroke:#9a6700,stroke-width:2px,color:#3b2300
classDef own fill:#ffffff,stroke:#0969da,stroke-width:2px,color:#0a3069
classDef ownev fill:#fff8e1,stroke:#9a6700,stroke-width:2px,color:#3b2300
classDef other fill:#f6f8fa,stroke:#8c959f,color:#24292f
classDef ext fill:#f6f8fa,stroke:#8c959f,stroke-dasharray:4 3,color:#57606a
classDef more fill:#ffffff,stroke:#d0d7de,stroke-dasharray:2 2,color:#57606a"""

MAP_STYLE_DIFF = """\
classDef engine fill:#f6f8fa,stroke:#d0d7de,color:#57606a
classDef own fill:#f6f8fa,stroke:#d0d7de,color:#57606a
classDef ownev fill:#f6f8fa,stroke:#d0d7de,color:#57606a
classDef other fill:#f6f8fa,stroke:#d0d7de,color:#57606a
classDef ext fill:#f6f8fa,stroke:#d0d7de,stroke-dasharray:4 3,color:#57606a
classDef more fill:#ffffff,stroke:#d0d7de,stroke-dasharray:2 2,color:#57606a
""" + STYLE_DIFF


def mid_node(s: str) -> str:
    return "m_" + re.sub(r"[^A-Za-z0-9_]", "_", s)


def short(mid: str) -> str:
    """Enemy.Died -> Died ;  Spawner.Spawn(Enemy,Vector2,float) -> Spawn(3)"""
    name = mid.split("(")[0]
    parts = name.split(".")
    base = parts[-1] if parts[-1] not in ("get", "set", "init") else parts[-2] + " · " + parts[-1]
    if "(" in mid:
        n = mid.count(",") + 1 if mid[mid.index("(") + 1 : -1] else 0
        base += f"({n})"
    return base


class MapBuilder:
    """A small node/edge collector that writes one Mermaid map; ids are stable (derived from member ids)."""

    def __init__(self, direction: str = "LR", diff: bool = False):
        self.direction = direction
        self.diff = diff
        self.groups: dict[str, dict] = {}  # group id -> {'title': str, 'nodes': [node ids], 'cls': str}
        self.nodes: dict[str, tuple] = {}  # node id -> (label, cls, shape)
        self.edges: list = []  # (a, b, label, style, status)
        self.status: dict[str, str] = {}

    def group(self, gid: str, title: str, order: int = 0):
        self.groups.setdefault(gid, {"title": title, "nodes": [], "order": order})
        return gid

    def node(self, nid: str, label: str, cls: str, group: str | None = None, shape: str = "rect", status: str = "same"):
        if nid not in self.nodes:
            self.nodes[nid] = (label, cls, shape)
            if group:
                self.groups[group]["nodes"].append(nid)
        if status != "same":
            self.status[nid] = status
        return nid

    def edge(self, a: str, b: str, label: str = "", style: str = "call", status: str = "same"):
        key = (a, b, label, style)
        if not any(e[:4] == key for e in self.edges):
            self.edges.append((a, b, label, style, status))

    def render(self) -> str:
        out = [MAP_CONFIG, f"flowchart {self.direction}"]
        grouped = {n for g in self.groups.values() for n in g["nodes"]}

        def node_line(nid, ind="  "):
            label, cls, shape = self.nodes[nid]
            q = f'"{label}"'
            s = {"rect": f"[{q}]", "round": f"({q})", "stadium": f"([{q}])", "sub": f"[[{q}]]", "db": f"[({q})]",
                 "flag": f">{q}]"}[shape]
            return f"{ind}{nid}{s}"

        for nid in self.nodes:
            if nid not in grouped:
                out.append(node_line(nid))
        for gid, g in sorted(self.groups.items(), key=lambda kv: (kv[1]["order"], kv[0])):
            if not g["nodes"]:
                continue
            out.append(f'  subgraph {gid}["{g["title"]}"]')
            for nid in g["nodes"]:
                out.append(node_line(nid, "    "))
            out.append("  end")
        styles = []
        for i, (a, b, label, style, status) in enumerate(self.edges):
            arrow = {"call": "-->", "write": "-.->", "create": "-.->", "engine": "-->", "inherit": "==>",
                     "data": "-.-"}[style]
            lab = f"|{label}|" if label else ""
            out.append(f"  {a} {arrow}{lab} {b}")
            if self.diff and status == "added":
                styles.append(f"  linkStyle {i} {EDGE_ADDED}")
            elif self.diff and status == "removed":
                styles.append(f"  linkStyle {i} {EDGE_REMOVED}")
        out += styles
        out += ["  " + l for l in (MAP_STYLE_DIFF if self.diff else MAP_STYLE).splitlines()]
        by_cls: dict[str, list] = {}
        for nid, (label, cls, shape) in self.nodes.items():
            c = self.status.get(nid, cls) if self.diff and self.status.get(nid) else cls
            by_cls.setdefault(c, []).append(nid)
        for c in sorted(by_cls):
            out.append(f"  class {','.join(by_cls[c])} {c}")
        for gid, g in self.groups.items():
            if g["nodes"]:
                out.append(f"  style {gid} fill:transparent,stroke:#d0d7de,color:#57606a")
        return "\n".join(out)
