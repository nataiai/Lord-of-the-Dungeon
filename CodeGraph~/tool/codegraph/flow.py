"""Statement tree -> flowchart (nodes + edges). Every statement of the method lands in exactly one node.

Conventions (see DESIGN.md):
- consecutive plain statements are one box (a basic block), at most MAX_BLOCK lines
- if / switch: a decision node; loops: a loop node with a back edge; return / yield break / throw: an exit node
- yield return (coroutines): a wait node, the flow goes on after it
- diff mode: statements carry status same/added/removed/modified; a removed compound statement is one inline ghost node
"""
from __future__ import annotations

from dataclasses import dataclass, field

from .csparse import Stmt, short_hash

MAX_BLOCK = 4


@dataclass
class Line:
    text: str
    status: str = "same"
    comment: str = ""
    line: int = 0
    old: str | None = None


@dataclass
class FNode:
    id: str
    kind: str  # start|end|block|decision|switch|loop|exit|wait|ghost|try
    lines: list = field(default_factory=list)  # [Line]
    calls: list = field(default_factory=list)  # project member ids called here
    line: int = 0
    end: int = 0

    @property
    def status(self) -> str:
        st = {l.status for l in self.lines}
        if not st or st == {"same"}:
            return "same"
        if len(st) == 1:
            return st.pop()
        return "modified"

    @property
    def text(self) -> str:
        return "\n".join(l.text for l in self.lines)


@dataclass
class FEdge:
    src: str
    dst: str
    label: str = ""
    kind: str = "seq"  # seq|yes|no|case|each|back|done
    status: str = "same"


@dataclass
class Flow:
    nodes: list = field(default_factory=list)
    edges: list = field(default_factory=list)

    def node(self, nid: str) -> FNode:
        return next(n for n in self.nodes if n.id == nid)

    @property
    def size(self) -> int:
        return len(self.nodes)


class FlowBuilder:
    def __init__(self, title: str, start_kind: str = "start"):
        self.f = Flow()
        self.title = title
        self.start_kind = start_kind
        self.breakables: list = []  # stack of {'kind': 'loop'|'switch', 'head': id, 'breaks': [...]}
        self._counts: dict = {}
        self._stack_after: list = []  # nodes the next new node must be drawn below (invisible layout links)

    # ------------------------------------------------------------------ ids
    def _id(self, kind: str, text: str) -> str:
        key = kind + "|" + text
        occ = self._counts.get(key, 0)
        self._counts[key] = occ + 1
        return "n" + short_hash(f"{key}|{occ}", 6)

    def _new(self, kind: str, lines: list, stmt: Stmt | None = None) -> FNode:
        n = FNode(id="", kind=kind, lines=lines, line=stmt.line if stmt else 0, end=stmt.end if stmt else 0)
        if stmt is not None:
            n.calls = [mid for k, mid in stmt.uses if k in ("call", "base")]
        self.f.nodes.append(n)
        if self._stack_after:  # else-if / case chains: keep each branch body beside its own check, not in one wide row
            for src in self._stack_after:
                self._edge(src, n, "", "invisible")
            self._stack_after = []
        return n

    def _edge(self, src: FNode, dst: FNode, label: str = "", kind: str = "seq"):
        e = FEdge("", "", label, kind)
        e._src, e._dst = src, dst  # ids and status are set in build(), once every node is final
        self.f.edges.append(e)

    def _connect(self, pend, dst: FNode):
        for src, label, kind in pend:
            self._edge(src, dst, label, kind)

    # ------------------------------------------------------------------ build
    def build(self, stmts: list) -> Flow:
        s = self._new(self.start_kind, [Line(self.title)])
        pend = [(s, "", "seq")]
        pend = self._seq(stmts, pend)
        if pend:
            e = self._new("end", [Line("end")])
            self._connect(pend, e)
        # stable ids: content based, so inserting a step elsewhere doesn't rename the others
        for n in self.f.nodes:
            if n.kind == self.start_kind:
                n.id = "nS"
            elif n.kind == "end":
                n.id = "nE"
            elif n.kind == "join":
                n.id = self._id("join", str(sum(1 for x in self.f.nodes[: self.f.nodes.index(n)] if x.kind == "join")))
            else:
                n.id = self._id(n.kind, n.text)
        # Mermaid places the first edge of a node first: for guard clauses put the main path ("no") before the exit
        for d in [n for n in self.f.nodes if getattr(n, "side_exit", False)]:
            yes = next((i for i, e in enumerate(self.f.edges) if e._src is d and e.kind == "yes"), None)
            no = next((i for i, e in enumerate(self.f.edges) if e._src is d and e.kind == "no"), None)
            if yes is not None and no is not None and yes < no:
                self.f.edges.insert(no, self.f.edges.pop(yes))
        for e in self.f.edges:
            a, b = e._src, e._dst
            e.src, e.dst = a.id, b.id
            if "removed" in (a.status, b.status):
                e.status = "removed"  # into / out of something that is gone
            elif b.status == "added" or (a.status == "added" and e.kind != "seq") or (a.status == "added" and b.kind == "end"):
                e.status = "added"  # a new path
            del e._src, e._dst
        return self.f

    def _seq(self, stmts, pend):
        for st in stmts:
            pend = self._stmt(st, pend)
        return pend

    def _open_block(self, pend) -> FNode | None:
        if len(pend) == 1:
            n, label, kind = pend[0]
            if n.kind == "block" and label == "" and kind == "seq" and n is self.f.nodes[-1] and len(n.lines) < MAX_BLOCK:
                return n
        return None

    def _stmt(self, st: Stmt, pend):
        k = st.kind
        if st.status == "removed" and k not in ("simple",):
            g = self._new("ghost", [Line(_summary(st), "removed", st.comment, st.line)], st)
            self._connect(pend, g)
            return [(g, "", "seq")]
        if st.status == "added":
            _mark(st, "added")
        if k in ("simple", "localfunc"):
            line = Line(st.text if k == "simple" else f"local function {st.text}(…)", st.status, st.comment, st.line, st.old_text)
            b = self._open_block(pend)
            if b is not None:
                b.lines.append(line)
                b.end = st.end
                b.calls += [mid for kk, mid in st.uses if kk in ("call", "base") and mid not in b.calls]
                return pend
            b = self._new("block", [line], st)
            self._connect(pend, b)
            return [(b, "", "seq")]
        if k == "if":
            parts = dict(st.parts)
            yes = parts.get("yes", [])
            if "no" not in parts and len(yes) == 1 and yes[0].kind == "simple":
                # `if (c) oneStatement;` -> one "guarded step" node, the flow goes straight on
                inner = yes[0]
                g = self._new("guard", [Line("if " + st.text, st.status, "", st.line, st.old_text),
                                        Line("→ " + inner.text, inner.status, inner.comment or st.comment, inner.line,
                                             ("→ " + inner.old_text) if inner.old_text else None)], st)
                g.calls += [mid for kk, mid in inner.uses if kk in ("call", "base") and mid not in g.calls]
                self._connect(pend, g)
                return [(g, "", "seq")]
            d = self._new("decision", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            self._connect(pend, d)
            exits_only = bool(yes) and yes[-1].kind in ("return", "yieldbreak", "throw", "break", "continue") and "no" not in parts
            if exits_only:  # guard clause: keep the main path straight, the exit goes to the side
                d.side_exit = True
                self._seq(yes, [(d, "yes", "yes")])
                return [(d, "no", "no")]
            out = self._seq(yes, [(d, "yes", "yes")])
            if "no" in parts:
                if parts["no"] and parts["no"][0].kind == "if":
                    self._stack_after = [src for src, _, _ in out]
                out += self._seq(parts["no"], [(d, "no", "no")])
            else:
                out.append((d, "no", "no"))
            return out
        if k == "switch":
            # A vertical chain of case checks (same logic as a fan-out, but narrow enough for GitHub). Each case
            # body ends in a small junction and the junctions form a rail down the side (DRAKON "select"):
            # every merge stays short instead of all branches crossing the chart to one point at the bottom.
            d = self._new("switch", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            self._connect(pend, d)
            prev, rail = [(d, "", "seq")], None
            for label, body in st.parts:
                if rail is not None:
                    self._stack_after = [rail]
                self.breakables.append({"kind": "switch", "head": d, "breaks": []})
                if label == "default":
                    o = self._seq(body, [(src, "other" if lab == "no" else lab, kind) for src, lab, kind in prev])
                    prev = []
                else:
                    c = self._new("case", [Line("case " + label, st.status, "", st.line)], st)
                    c.calls = []
                    self._connect(prev, c)
                    o = self._seq(body, [(c, "yes", "yes")])
                    prev = [(c, "no", "no")]
                o += self.breakables.pop()["breaks"]
                if o:
                    j = self._new("join", [], None)
                    self._connect(o, j)
                    if rail is not None:
                        self._edge(rail, j)
                    rail = j
            if prev and rail is not None:  # no default: "no match" joins the rail too
                self._connect([(s, "other" if l == "no" else l, kk) for s, l, kk in prev], rail)
                prev = []
            return ([(rail, "", "seq")] if rail is not None else []) + prev
        if k == "loop" or k == "using":
            if k == "using":
                b = self._new("block", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
                self._connect(pend, b)
                return self._seq(st.parts[0][1], [(b, "", "seq")])
            L = self._new("loop", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            self._connect(pend, L)
            self.breakables.append({"kind": "loop", "head": L, "breaks": []})
            body_out = self._seq(st.parts[0][1], [(L, "each", "each")])
            for src, lab, kind in body_out:
                self._edge(src, L, "", "back")
            lp = self.breakables.pop()
            return [(L, "done", "done")] + lp["breaks"]
        if k == "do":
            first_index = len(self.f.nodes)
            body_out = self._seq(st.parts[0][1], pend)
            d = self._new("decision", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            self._connect(body_out, d)
            if len(self.f.nodes) > first_index + 1:
                self._edge(d, self.f.nodes[first_index], "yes", "back")
            return [(d, "no", "no")]
        if k in ("return", "yieldbreak", "throw"):
            r = self._new("exit", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            self._connect(pend, r)
            return []
        if k == "yield":
            y = self._new("wait", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            self._connect(pend, y)
            return [(y, "", "seq")]
        if k == "break":
            if self.breakables:
                self.breakables[-1]["breaks"] += [(s, l or "break", kk) for s, l, kk in pend]
                return []
            return pend
        if k == "continue":
            loop = next((b for b in reversed(self.breakables) if b["kind"] == "loop"), None)
            if loop is not None:
                for src, lab, kind in pend:
                    self._edge(src, loop["head"], "continue", "back")
                return []
            return pend
        if k == "try":
            t = self._new("try", [Line("try", st.status, "", st.line)], st)
            self._connect(pend, t)
            outs = []
            fin = None
            for label, body in st.parts:
                if label == "finally":
                    fin = body
                    continue
                outs += self._seq(body, [(t, "" if label == "try" else label, "seq" if label == "try" else "case")])
            if fin is not None:
                outs = self._seq(fin, outs)
            return outs
        b = self._new("block", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
        self._connect(pend, b)
        return [(b, "", "seq")]


def _mark(st: Stmt, status: str):
    st.status = status
    for _, sub in st.parts:
        for s in sub:
            _mark(s, status)


def _count(stmts) -> int:
    return sum(1 + sum(_count(sub) for _, sub in s.parts) for s in stmts)


def _summary(st: Stmt) -> str:
    inner = sum(_count(sub) for _, sub in st.parts)
    word = {"if": "if", "switch": "", "loop": "", "try": "", "using": "", "do": "do … "}.get(st.kind, "")
    head = (word + " " + st.text).strip() if st.kind == "if" else st.text
    return f"{head} {{ {inner} step{'s' if inner != 1 else ''} }}" if inner else head


def build_flow(title: str, stmts: list, start_kind: str = "start") -> Flow:
    return FlowBuilder(title, start_kind).build(stmts)
