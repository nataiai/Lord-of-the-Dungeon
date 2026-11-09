"""Statement tree -> a short, readable flowchart. Every statement still lands in exactly one node.

A function mostly does two things: it calls other (complex) functions, or it does simple calculation.
So the chart shows:
- calc   : simple steps (assignments, math, small helpers, ifs/loops that only do that) folded into ONE box
- call   : a call to a complex function (one that branches, loops or calls others), or to external code: own box
- guard  : `if (c) call();` -> one box "if c -> call()"
- decision / loop / switch : only when a branch leads to a call, an exit or a wait
- exit   : return / yield break / throw, drawn where it happens;  wait : yield return (coroutines)
The AI writes the short text of each box (notes), keyed by the box id; the exact code goes in a table below the chart.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field

from .csparse import Stmt, short_hash

MAX_CALC = 40  # statements folded into one calc box (its text is a short label; the code is in a table)


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
    kind: str  # start|end|calc|call|guard|decision|switch|case|join|loop|exit|wait|ghost|try
    lines: list = field(default_factory=list)  # [Line] the code, for the code table
    calls: list = field(default_factory=list)  # project member ids called here (complex ones)
    line: int = 0
    end: int = 0
    brief: str = ""  # generated short text (used when there's no AI label)
    actions: list = field(default_factory=list)  # engine actions done here (Destroy, Instantiate, ...)
    purpose: str = ""  # call boxes: what the called function does (its one-line note), shown under the label
    label: str = ""  # AI label (pages.py fills it from the notes)

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
    kind: str = "seq"  # seq|yes|no|case|each|back|done|invisible
    status: str = "same"


@dataclass
class Flow:
    nodes: list = field(default_factory=list)
    edges: list = field(default_factory=list)
    groups: list = field(default_factory=list)  # [(group id, title, [node ids])]: a short callee drawn inside

    def node(self, nid: str) -> FNode:
        return next(n for n in self.nodes if n.id == nid)

    @property
    def size(self) -> int:
        return len(self.nodes)

    @property
    def steps(self) -> list:  # the nodes a reader reads (no start/end/junctions)
        return [n for n in self.nodes if n.kind not in ("start", "end", "join")]


class Classifier:
    """Decides what is 'simple calculation' and what is 'a call to a complex function'."""

    def __init__(self, complex_ids: set = frozenset(), notable: tuple = (), complex_body: set | None = None):
        self.complex_ids = complex_ids  # complex when called normally (any override may run)
        self.complex_body = complex_body if complex_body is not None else complex_ids  # for `base.F()` calls
        self.notable = notable  # engine calls worth their own box (Destroy, Instantiate, StartCoroutine, ...)

    def calls(self, st: Stmt) -> list:
        return [t for k, t in st.uses if (k == "call" and t in self.complex_ids) or (k == "base" and t in self.complex_body)]

    def is_call(self, st: Stmt) -> bool:
        if st.kind != "simple":
            return False
        if self.calls(st):
            return True
        return any(re.search(r"\b" + re.escape(n) + r"\s*[(<]", st.text) for n in self.notable)

    def actions(self, st: Stmt) -> list:
        """Engine actions in this statement (Destroy, Instantiate, ...): named on the box like a call."""
        return [n for n in self.notable if re.search(r"\b" + re.escape(n) + r"\s*[(<]", st.text)]

    def simple(self, st: Stmt) -> bool:
        """True when the whole statement (with its body) is only calculation."""
        if st.kind in ("return", "yieldbreak", "throw", "yield", "break", "continue", "localfunc", "try", "do"):
            return False
        if st.kind == "simple":
            return not self.is_call(st)
        if self.calls(st):  # a complex call inside the condition / header
            return False
        return all(self.simple(s) for _, sub in st.parts for s in sub)


# ----------------------------------------------------------------------------- short generated text

_LHS = re.compile(r"^(?:var\s+|[\w<>\[\],.?]+\s+)?([\w.\[\]]+?)\s*(?:[-+*/%|&]?=)(?!=)")
_CALL = re.compile(r"([A-Za-z_]\w*)\s*(?:<[^()]*>)?\s*\(")


def _names(st: Stmt, out: list):
    if st.kind == "simple":
        m = _LHS.match(st.text)
        if m:
            out.append(re.sub(r"\[.*?\]", "", m.group(1)).split(".")[-1])
        else:
            c = _CALL.search(st.text)
            if c and c.group(1) not in ("new", "if"):
                out.append(c.group(1) + "()")
    for _, sub in st.parts:
        for s in sub:
            _names(s, out)


def brief_calc(stmts: list) -> str:
    names = []
    for s in stmts:
        _names(s, names)
    names = list(dict.fromkeys(n for n in names if n))
    if not names:
        return "simple steps"
    sets = [n for n in names if not n.endswith("()")]
    does = [n for n in names if n.endswith("()")]
    parts = []
    if sets:
        parts.append("set " + ", ".join(sets[:3]) + (f" +{len(sets) - 3}" if len(sets) > 3 else ""))
    if does:
        parts.append(", ".join(does[:2]) + (f" +{len(does) - 2}" if len(does) > 2 else ""))
    return " · ".join(parts)


def short_code(text: str, n: int = 40) -> str:
    text = re.sub(r"\s+", " ", text).strip()
    return text if len(text) <= n else text[: n - 1].rstrip() + "…"


def brief_call(st: Stmt, callee_names: list) -> str:
    m = _LHS.match(st.text)
    found = _CALL.search(st.text)
    name = callee_names[0] if callee_names else (found.group(1) if found else "")
    call = f"{name}(…)" if name else short_code(st.text)
    if m:
        lhs = re.sub(r"\[.*?\]", "", m.group(1)).split(".")[-1]
        return f"{lhs} = {call}"
    return call


# ----------------------------------------------------------------------------- builder

class FlowBuilder:
    def __init__(self, title: str, start_kind: str = "start", classifier: Classifier | None = None,
                 short_name=lambda mid: mid.split(".")[-1], ghosts: bool = True):
        self.ghosts = ghosts  # union diff: a removed if/loop is one inline ghost box. False: draw it normally
        self.f = Flow()
        self.title = title
        self.start_kind = start_kind
        self.cls = classifier or Classifier()
        self.short_name = short_name
        self.breakables: list = []
        self._counts: dict = {}
        self._stack_after: list = []

    def _id(self, kind: str, text: str) -> str:
        key = kind + "|" + text
        occ = self._counts.get(key, 0)
        self._counts[key] = occ + 1
        return "n" + short_hash(f"{key}|{occ}", 6)

    def _new(self, kind: str, lines: list, stmt: Stmt | None = None, calls: list | None = None) -> FNode:
        n = FNode(id="", kind=kind, lines=lines, line=stmt.line if stmt else 0, end=stmt.end if stmt else 0)
        n.calls = list(calls or [])
        self.f.nodes.append(n)
        if self._stack_after:  # else-if / case chains: keep each body beside its own check, not in one wide row
            for src in self._stack_after:
                self._edge(src, n, "", "invisible")
            self._stack_after = []
        return n

    def _edge(self, src: FNode, dst: FNode, label: str = "", kind: str = "seq"):
        e = FEdge("", "", label, kind)
        e._src, e._dst = src, dst
        self.f.edges.append(e)

    def _connect(self, pend, dst: FNode):
        for src, label, kind in pend:
            self._edge(src, dst, label, kind)

    def build(self, stmts: list) -> Flow:
        s = self._new(self.start_kind, [Line(self.title)])
        s.brief = self.title
        pend = self._seq(stmts, [(s, "", "seq")])
        if pend:
            e = self._new("end", [Line("end")])
            e.brief = "end"
            self._connect(pend, e)
        # guard clauses: emit the main path ("no") before the exit, so the main path stays straight
        for d in [n for n in self.f.nodes if getattr(n, "side_exit", False)]:
            yes = next((i for i, e in enumerate(self.f.edges) if e._src is d and e.kind == "yes"), None)
            no = next((i for i, e in enumerate(self.f.edges) if e._src is d and e.kind == "no"), None)
            if yes is not None and no is not None and yes < no:
                self.f.edges.insert(no, self.f.edges.pop(yes))
        joins = 0
        for n in self.f.nodes:  # content-based ids: an edit elsewhere doesn't rename this box (or orphan its label)
            if n.kind == self.start_kind:
                n.id = "nS"
            elif n.kind == "end":
                n.id = "nE"
            elif n.kind == "join":
                n.id = self._id("join", str(joins))
                joins += 1
            else:
                n.id = self._id(n.kind, n.text)
        for e in self.f.edges:
            a, b = e._src, e._dst
            e.src, e.dst = a.id, b.id
            if "removed" in (a.status, b.status):
                e.status = "removed"
            elif b.status == "added" or (a.status == "added" and e.kind != "seq") or (a.status == "added" and b.kind == "end"):
                e.status = "added"
            del e._src, e._dst
        return self.f

    def _seq(self, stmts, pend):
        for st in stmts:
            pend = self._stmt(st, pend)
        return pend

    def _open_calc(self, pend) -> FNode | None:
        if len(pend) == 1:
            n, label, kind = pend[0]
            if n.kind == "calc" and label == "" and kind == "seq" and n is self.f.nodes[-1] and len(n.lines) < MAX_CALC:
                return n
        return None

    def _line(self, st: Stmt, text: str | None = None) -> Line:
        old = st.old_text if st.kind == "simple" else (st.old_text + " …" if st.old_text else None)
        return Line(text if text is not None else (st.full or st.text), st.status, st.comment, st.line, old)

    def _calc(self, st: Stmt, pend):
        c = self._open_calc(pend)
        if c is None:
            c = self._new("calc", [], st)
            c._stmts = []
            self._connect(pend, c)
            pend = [(c, "", "seq")]
        c.lines.append(self._line(st))
        c._stmts.append(st)
        c.end = st.end
        c.brief = brief_calc(c._stmts)
        return pend

    def _stmt(self, st: Stmt, pend):
        k = st.kind
        if st.status == "added":
            _mark(st, "added")
        if self.cls.simple(st):
            return self._calc(st, pend)
        if st.status == "removed" and k != "simple" and self.ghosts:
            g = self._new("ghost", [self._line(st, _summary(st))], st)
            g.brief = short_code(_summary(st))
            self._connect(pend, g)
            return [(g, "", "seq")]
        if k == "simple" or k == "localfunc":
            calls = self.cls.calls(st)
            n = self._new("call", [self._line(st)], st, calls)
            n.actions = self.cls.actions(st)
            n.brief = brief_call(st, [self.short_name(c) for c in calls] + n.actions)
            self._connect(pend, n)
            return [(n, "", "seq")]
        if k == "if":
            parts = dict(st.parts)
            yes = parts.get("yes", [])
            if "no" not in parts and len(yes) == 1 and yes[0].kind == "simple" and not self.cls.calls(st):
                inner = yes[0]  # `if (c) call();` -> one box
                calls = self.cls.calls(inner)
                g = self._new("guard", [Line("if " + st.text, st.status, "", st.line, st.old_text),
                                        self._line(inner)], st, calls)
                g.actions = self.cls.actions(inner)
                g.brief = f"if {short_code(st.text, 26)} → {brief_call(inner, [self.short_name(c) for c in calls] + g.actions)}"
                self._connect(pend, g)
                return [(g, "", "seq")]
            d = self._new("decision", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st,
                          self.cls.calls(st))
            d.brief = short_code(st.text, 36) + " ?"
            self._connect(pend, d)
            exits_only = bool(yes) and yes[-1].kind in ("return", "yieldbreak", "throw", "break", "continue") and "no" not in parts
            if exits_only:
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
        if k == "switch":  # a vertical chain of case checks; bodies merge on a rail of small dots
            d = self._new("switch", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            d.brief = short_code(st.text, 36)
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
                    c.brief = short_code(label, 30) + " ?"
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
            if prev and rail is not None:
                self._connect([(s, "other" if l == "no" else l, kk) for s, l, kk in prev], rail)
                prev = []
            return ([(rail, "", "seq")] if rail is not None else []) + prev
        if k == "using":
            b = self._new("calc", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st)
            b.brief = short_code(st.text)
            self._connect(pend, b)
            return self._seq(st.parts[0][1], [(b, "", "seq")])
        if k == "loop":
            L = self._new("loop", [Line(st.text, st.status, st.comment, st.line, st.old_text)], st, self.cls.calls(st))
            L.brief = short_code(st.text, 36)
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
            d.brief = short_code(st.text, 36) + " ?"
            self._connect(body_out, d)
            if len(self.f.nodes) > first_index + 1:
                self._edge(d, self.f.nodes[first_index], "yes", "back")
            return [(d, "no", "no")]
        if k in ("return", "yieldbreak", "throw"):
            r = self._new("exit", [self._line(st)], st, self.cls.calls(st))
            r.brief = short_code(st.text, 30)
            self._connect(pend, r)
            return []
        if k == "yield":
            y = self._new("wait", [self._line(st)], st, self.cls.calls(st))
            y.brief = short_code(st.text.replace("yield return ", "wait: ", 1).replace("yield ", "wait: ", 1), 36)
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
            t.brief = "try"
            self._connect(pend, t)
            outs, fin = [], None
            for label, body in st.parts:
                if label == "finally":
                    fin = body
                    continue
                outs += self._seq(body, [(t, "" if label == "try" else label, "seq" if label == "try" else "case")])
            if fin is not None:
                outs = self._seq(fin, outs)
            return outs
        return self._calc(st, pend)


def _mark(st: Stmt, status: str):
    st.status = status
    for _, sub in st.parts:
        for s in sub:
            _mark(s, status)


def _count(stmts) -> int:
    return sum(1 + sum(_count(sub) for _, sub in s.parts) for s in stmts)


def _summary(st: Stmt) -> str:
    inner = sum(_count(sub) for _, sub in st.parts)
    head = ("if " + st.text) if st.kind == "if" else st.text
    return f"{head} {{ {inner} step{'s' if inner != 1 else ''} }}" if inner else head


def covered(flow: Flow) -> int:
    """How many statements the flow's boxes hold (a folded `if` counts with everything inside it)."""
    total = 0
    for n in flow.nodes:
        if n.kind in ("start", "end", "join", "case"):
            continue
        if n.kind == "calc" and hasattr(n, "_stmts"):
            total += count_statements(n._stmts)
        else:
            total += len(n.lines)
    return total


def count_statements(stmts) -> int:
    """Statements a flow must account for (break / continue are edges, not boxes)."""
    return sum((0 if s.kind in ("break", "continue") else 1) + count_statements([x for _, sub in s.parts for x in sub])
               for s in stmts)


def build_flow(title: str, stmts: list, start_kind: str = "start", classifier: Classifier | None = None,
               short_name=lambda mid: mid.split(".")[-1], ghosts: bool = True) -> Flow:
    return FlowBuilder(title, start_kind, classifier, short_name, ghosts).build(stmts)
