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
  flowchart:
    wrappingWidth: 260
    nodeSpacing: 26
    rankSpacing: 30
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
# --- palette: structure. Colors people read without a legend: green = start, yellow = a question,
# blue = goes to another function (like a link), red = stop / remove, white / gray = plain steps.
STYLE_BROWSE = """\
classDef unity fill:#dafbe1,stroke:#1a7f37,stroke-width:2px,color:#0f3d1d
classDef start fill:#dafbe1,stroke:#1a7f37,stroke-width:2px,color:#0f3d1d
classDef calc fill:#ffffff,stroke:#d0d7de,color:#57606a
classDef calls fill:#ddf4ff,stroke:#0969da,stroke-width:2px,color:#0a3069
classDef ext fill:#ddf4ff,stroke:#0969da,stroke-width:2px,stroke-dasharray:5 3,color:#0a3069
classDef cond fill:#fff8c5,stroke:#bf8700,stroke-width:1.5px,color:#3d2a00
classDef loop fill:#f6f8fa,stroke:#57606a,color:#1f2328
classDef exit fill:#ffebe9,stroke:#cf222e,color:#82071e
classDef kill fill:#ffebe9,stroke:#cf222e,stroke-width:2px,color:#82071e
classDef spawn fill:#dafbe1,stroke:#1a7f37,stroke-width:2px,color:#0f3d1d
classDef wait fill:#fff1e5,stroke:#bc4c00,color:#4d1d00
classDef fin fill:#eaeef2,stroke:#8c959f,color:#57606a
classDef join fill:#d0d7de,stroke:#8c959f,color:#57606a
classDef ghdr fill:#f5f9ff,stroke:#f5f9ff,color:#0a3069,font-weight:bold"""
EDGE_YES = "stroke:#1a7f37,stroke-width:2px,color:#1a7f37"
EDGE_NO = "stroke:#cf222e,stroke-width:1.5px,color:#cf222e"

# --- palette: diff. The colors everyone already reads in git / GitHub: green new, red removed, yellow changed.
# The + / − / ~ marks stay as a backup for color-blind readers.
STYLE_DIFF = """\
classDef same fill:#ffffff,stroke:#d0d7de,color:#57606a
classDef added fill:#dafbe1,stroke:#1a7f37,stroke-width:3px,color:#0f3d1d
classDef removed fill:#ffebe9,stroke:#cf222e,stroke-width:3px,color:#82071e
classDef modified fill:#fff8c5,stroke:#bf8700,stroke-width:3px,color:#4d2d00
classDef impacted fill:#ffffff,stroke:#6e7781,stroke-width:2px,stroke-dasharray:2 3,color:#24292f"""

EDGE_ADDED = "stroke:#1a7f37,stroke-width:3px"
EDGE_REMOVED = "stroke:#cf222e,stroke-width:3px"

MARK = {"added": "+ ", "removed": "− ", "modified": "~ ", "same": ""}


def esc(text: str) -> str:
    """Text safe inside a quoted Mermaid label (entity codes, so nothing is read as HTML or syntax)."""
    text = text.replace("#", "#35;")
    return (text.replace("&", "#38;").replace('"', "#34;").replace("<", "#60;").replace(">", "#62;")
            .replace("`", "#96;").replace("|", "#124;"))


def wrap(text: str, width: int = WRAP, max_lines: int = MAX_LINES_PER_STMT, indent: str = "  ") -> list:
    """Break long text at spaces / after commas and dots, never inside a word."""
    if len(text) <= width:
        return [text]
    tokens = re.findall(r"[^ ,.(]+[ ,.(]?|[ ,.(]", text)
    lines, cur = [], ""
    for tok in tokens:
        if len(cur) + len(tok) > width and cur.strip():
            lines.append(cur.rstrip())
            cur = indent + tok.lstrip()
        else:
            cur += tok
    if cur.strip():
        lines.append(cur.rstrip())
    if len(lines) > max_lines:
        lines = lines[:max_lines]
        lines[-1] = lines[-1][: width - 1] + " …"
    return lines


# ----------------------------------------------------------------------------- flowcharts

LABEL_WRAP = 30  # labels are short plain sentences (AI-written, or generated); code lives in the table below the chart


def wrap_words(text: str, width: int, max_lines: int) -> list:
    """Break a plain sentence at spaces only, so names like `PathFinding.Direction()` stay whole."""
    lines, cur = [], ""
    for word in text.split():
        if cur and len(cur) + 1 + len(word) > width:
            lines.append(cur)
            cur = word
        else:
            cur = f"{cur} {word}" if cur else word
    if cur:
        lines.append(cur)
    if len(lines) > max_lines:
        lines = lines[:max_lines]
        lines[-1] = lines[-1].rstrip() + " …"
    return lines


def node_text(node: FNode, short_name=lambda mid: mid.split(".")[-1]) -> str:
    """What a box says: the AI label, else a generated summary (the code itself is in the table below the chart)."""
    text = node.label or node.brief
    if node.kind in ("call", "guard") and (node.calls or node.actions):  # a call box always names what it calls
        names = list(dict.fromkeys([short_name(c) for c in node.calls] + list(node.actions)))
        if not any(n in text for n in names):
            text += " → " + ", ".join(n + "()" for n in names[:2])
    return text


def _label(node: FNode, diff: bool, subtitle: str = "", short_name=None) -> str:
    text = node_text(node, short_name) if short_name else node_text(node)
    if node.kind == "loop":
        text = "🔁 " + text
    if node.kind == "wait" and not text.startswith("⏳"):
        text = "⏳ " + text
    if node.kind == "exit":
        bare = all(l.text.strip() in ("return", "yield break") for l in node.lines)
        text = ("⏹ " if bare else "↩ ") + text
    if node.kind in ("start", "unity") and not diff:
        text = "▶ " + text.lstrip("⚡ ").strip() if not text.startswith("▶") else text
    if node.kind in ("call", "guard") and "Destroy" in node.actions:
        text = "✖ " + text
    elif node.kind in ("call", "guard") and "Instantiate" in node.actions:
        text = "✚ " + text
    if diff:
        text = MARK[node.status] + text
    out = [esc(p) for p in wrap_words(text, LABEL_WRAP, 3)]
    if node.purpose and not diff:  # what the called function does, in small italics
        out += ["<i>" + esc(p) + "</i>" for p in wrap_words(node.purpose, LABEL_WRAP + 4, 2)]
    if subtitle:
        out.append("<i>" + esc(subtitle) + "</i>")
    return "<br>".join(out)


def _shape(node: FNode, label: str) -> str:
    k = node.kind
    q = f'"{label}"'
    if k in ("start", "end", "exit"):
        return f"([{q}])"
    if k in ("decision", "case"):  # the flowchart shape everyone reads as "a question"
        return "{" + q + "}"
    if k in ("switch", "try", "loop"):
        return "{{" + q + "}}"
    if k == "wait":
        return f"[/{q}/]"
    if k == "join":  # a merge point on a switch rail
        return '((" "))'
    if k == "header":  # title of a short function drawn inside the caller
        return f"[{q}]"
    if k in ("call", "guard") and (node.calls or node.actions):
        return f"[[{q}]]"
    return f"[{q}]"


def _browse_class(node: FNode, unity: bool) -> str:
    k = node.kind
    if k == "start":
        return "unity" if unity else "start"
    if k in ("call", "guard") and "Destroy" in node.actions:
        return "kill"
    if k in ("call", "guard") and "Instantiate" in node.actions:
        return "spawn"
    if k in ("call", "guard") and node.purpose == "AI/ (external)":
        return "ext"
    return {"end": "fin", "decision": "cond", "switch": "cond", "try": "cond", "case": "cond", "loop": "loop",
            "exit": "exit", "wait": "wait", "join": "join", "header": "ghdr"}.get(k, "calls" if k in ("call", "guard") else "calc")


def flow_to_mermaid(flow: Flow, *, unity_event: str | None = None, coroutine: bool = False, diff: bool = False,
                    config: bool = True, short_name=None) -> str:
    subtitle = ""
    if unity_event:
        subtitle = UNITY_WHEN.get(unity_event, "engine callback")
    elif coroutine:
        subtitle = "coroutine: can wait"
    lines = ([FLOW_CONFIG] if config else []) + ["flowchart TD"]
    # exits last: dagre then places them beside the main path, which stays straight (DRAKON)
    for n in [x for x in flow.nodes if x.kind != "exit"] + [x for x in flow.nodes if x.kind == "exit"]:
        lines.append(f"  {n.id}{_shape(n, _label(n, diff, subtitle if n.id == 'nS' else '', short_name))}")
    for gid, title, ids in getattr(flow, "groups", []):  # a short called function, drawn inside the caller
        t = title if len(title) <= 52 else title[:51].rstrip() + "…"
        lines.append(f'  subgraph {gid}["{esc(t)}"]')
        lines += [f"    {i}" for i in ids]
        lines.append("  end")
        lines.append(f"  style {gid} fill:#f5f9ff,stroke:#8cb4e8,stroke-dasharray:4 3,color:#0a3069")
    link_styles = []
    for i, e in enumerate(flow.edges):
        arrow = {"back": "-.->", "invisible": "~~~"}.get(e.kind, "-->")
        label = e.label or ("next" if e.kind == "back" else "")
        lab = f"|{esc(label)}|" if label and e.kind != "invisible" else ""
        lines.append(f"  {e.src} {arrow}{lab} {e.dst}")
        if not diff and e.label == "yes":
            link_styles.append(f"  linkStyle {i} {EDGE_YES}")
        elif not diff and e.label == "no":
            link_styles.append(f"  linkStyle {i} {EDGE_NO}")
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
classDef engine fill:#dafbe1,stroke:#1a7f37,stroke-width:2px,color:#0f3d1d
classDef own fill:#ddf4ff,stroke:#0969da,stroke-width:2px,color:#0a3069
classDef ownev fill:#ddf4ff,stroke:#1a7f37,stroke-width:2px,color:#0a3069
classDef other fill:#f6f8fa,stroke:#8c959f,color:#24292f
classDef ext fill:#f6f8fa,stroke:#8c959f,stroke-dasharray:5 3,color:#57606a
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
