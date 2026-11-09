"""Graph diff between two revisions: one union graph per view, changes colored, plus a ranked text list.

Matching (research: BinDiff / GumTree / RefactoringMiner):
  files by path; types by full name; members by id, then (same type + name) = signature changed,
  then identical code = renamed / moved; statements inside a matched method by sequence alignment per block
  (difflib), pairing leftover statements of the same kind when they are similar enough (= modified).
"""
from __future__ import annotations

import copy
import difflib
import os
import re
import subprocess

from .csparse import Stmt
from .flow import Classifier, build_flow
from .mermaid import MapBuilder, esc, flow_to_mermaid, mid_node, short
from .model import NOTABLE, Model, build_model, load_config, load_sources
from .notes import load_notes
from .pages import CALLS, GENERATED, LEGEND, display_name, esc_md, icon, per_frame

SIMILAR = 0.5  # text similarity for "the same statement, edited" (GumTree / git use 0.5 too)

RANK = {"crucial": 0, "medium": 1, "low": 2, "none": 3}
RANK_ICON = {"crucial": "🟥", "medium": "🟧", "low": "🟨", "none": "⬜"}


# ----------------------------------------------------------------------------- statement alignment

def _ratio(a: str, b: str) -> float:
    return difflib.SequenceMatcher(None, a, b, autojunk=False).ratio()


def _all(stmts):
    for s in stmts:
        yield s
        for _, sub in s.parts:
            yield from _all(sub)


def _mark(stmts, status):
    for s in _all(stmts):
        s.status = status


def _clone(stmts: list) -> list:
    """Copy statement trees (keeping the tree-sitter node references, which can't be deep-copied)."""
    out = []
    for s in stmts:
        c = copy.copy(s)
        c.parts = [(l, _clone(sub)) for l, sub in s.parts]
        out.append(c)
    return out


def _child_overlap(o: Stmt, n: Stmt) -> float:
    a = [s.sig for s in _all([x for _, sub in o.parts for x in sub])]
    b = [s.sig for s in _all([x for _, sub in n.parts for x in sub])]
    if not a and not b:
        return 1.0
    return difflib.SequenceMatcher(None, a, b, autojunk=False).ratio()


def _similar(o: Stmt, n: Stmt) -> bool:
    if o.kind != n.kind:
        return False
    r = _ratio(o.text, n.text)
    if o.parts or n.parts:
        return r >= 0.4 or _child_overlap(o, n) >= SIMILAR
    return r >= SIMILAR


def _merge(o: Stmt, n: Stmt, changed_header: bool) -> Stmt:
    m = copy.copy(n)
    m.status = "modified" if changed_header else "same"
    m.old_text = o.text if changed_header else None
    m.old_full = o.full if changed_header else None
    if o.parts or n.parts:
        labels = [l for l, _ in n.parts] + [l for l, _ in o.parts if l not in dict(n.parts)]
        od, nd = dict(o.parts), dict(n.parts)
        m.parts = [(l, align(od.get(l, []), nd.get(l, []))) for l in labels]
    return m


def align(old: list, new: list) -> list:
    """New statements with a status, and removed old ones kept where they were (a union tree)."""
    old = [copy.copy(s) for s in old]
    new = [copy.copy(s) for s in new]
    sm = difflib.SequenceMatcher(a=[s.sig for s in old], b=[s.sig for s in new], autojunk=False)
    out = []
    for op, i1, i2, j1, j2 in sm.get_opcodes():
        if op == "equal":
            out += [_merge(o, n, False) for o, n in zip(old[i1:i2], new[j1:j2])]
        elif op == "insert":
            add = _clone(new[j1:j2])
            _mark(add, "added")
            out += add
        elif op == "delete":
            rem = _clone(old[i1:i2])
            _mark(rem, "removed")
            out += rem
        else:
            olds, ptr = old[i1:i2], 0
            for n in new[j1:j2]:
                k = next((i for i in range(ptr, len(olds)) if _similar(olds[i], n)), None)
                if k is None:
                    add = _clone([n])
                    _mark(add, "added")
                    out += add
                    continue
                rem = _clone(olds[ptr:k])
                _mark(rem, "removed")
                out += rem
                out.append(_merge(olds[k], n, olds[k].text != n.text))
                ptr = k + 1
            rem = _clone(olds[ptr:])
            _mark(rem, "removed")
            out += rem
    return out


def split_union(stmts: list, cls: Classifier | None = None) -> tuple:
    """A union tree (from align) -> (before tree, after tree), each keeping the change marks for coloring.
    A step that has its own box on one side only (a call, decision, loop, exit) leaves a small dotted stub on the
    other side ("+1 new step here"), so the two charts stay alike (Blueprint Diff / BPMN diff tools do this).
    A plain line added to / removed from a gray box marks that box yellow on the other side instead."""
    before, after = [], []
    pending = {"added": [], "removed": []}

    def boxed(st):
        return not (cls.simple(st) if cls is not None else st.kind == "simple")

    def calc(st):
        return st is not None and st.kind != "stub" and not boxed(st)

    def flush(nxt_b=None, nxt_a=None):
        for status, tree, nxt in (("added", before, nxt_b), ("removed", after, nxt_a)):
            items = pending[status]
            if not items:
                continue
            prev = tree[-1] if tree else None
            near = prev if calc(prev) else nxt if calc(nxt) else None  # the gray box these lines join
            n = sum(1 for x in items if boxed(x) or near is None)
            if n:
                word = "new step" if status == "added" else "step"
                tail = "here" if status == "added" else "removed here"
                sign = "+" if status == "added" else "−"
                tree.append(Stmt("stub", f"{sign}{n} {word}{'s' if n > 1 else ''} {tail}", 0, 0))
            if near is not None and any(not boxed(x) for x in items) and near.status == "same":
                near.status = "modified"
            pending[status] = []

    for st in stmts:
        b = a = None
        if st.status != "added":
            b = copy.copy(st)
            if st.status == "modified" and st.old_text is not None:
                b.text, b.full = st.old_text, getattr(st, "old_full", None) or st.old_text
                b.old_text = None
            b.parts = [(l, split_union(sub, cls)[0]) for l, sub in st.parts]
        if st.status != "removed":
            a = copy.copy(st)
            a.parts = [(l, split_union(sub, cls)[1]) for l, sub in st.parts]
        if st.status in ("added", "removed"):
            pending[st.status].append(st)
        else:
            flush(b, a)
        if b is not None:
            before.append(b)
        if a is not None:
            after.append(a)
    flush()
    return before, after


def _changes(stmts) -> dict:
    c = {"added": 0, "removed": 0, "modified": 0, "cond": 0, "branch": 0}
    for s in _all(stmts):
        if s.status in c:
            c[s.status] += 1
        if s.status == "modified" and s.kind in ("if", "loop", "switch", "do"):
            c["cond"] += 1
        if s.status in ("added", "removed") and s.kind in ("if", "loop", "switch", "do", "return", "throw",
                                                               "yieldbreak"):
            c["branch"] += 1
    return c


# ----------------------------------------------------------------------------- model diff

class MemberChange:
    def __init__(self, mid, status, old=None, new=None, note=""):
        self.id, self.status, self.old, self.new, self.note = mid, status, old, new, note
        self.stmts = None
        self.counts = {}
        self.rank = "low"
        self.calls_added, self.calls_removed = [], []


def _sig(mi) -> str:
    return f"{mi.member.type} {mi.member.name}({', '.join(p.type for p in mi.member.params)})"


def diff_models(old: Model, new: Model) -> dict:
    out = {"members": [], "fields": [], "files": [], "external": []}
    om, nm = old.members, new.members
    matched_old = set()
    # 1. same id
    for mid, ni in nm.items():
        oi = om.get(mid)
        if oi is not None:
            matched_old.add(mid)
            if oi.member.fingerprint != ni.member.fingerprint:
                out["members"].append(MemberChange(mid, "modified", oi, ni))
    # 2. same type + name, other parameters = signature changed; 3. identical code = renamed / moved
    left_old = {k: v for k, v in om.items() if k not in matched_old and k not in nm}
    left_new = {k: v for k, v in nm.items() if k not in om}
    for nid, ni in list(left_new.items()):
        cand = next((oid for oid, oi in left_old.items() if oi.type.full == ni.type.full and oi.member.name == ni.member.name
                     and oi.member.kind == ni.member.kind), None)
        if cand is None:
            cand = next((oid for oid, oi in left_old.items() if oi.member.kind == ni.member.kind
                         and oi.member.fingerprint == ni.member.fingerprint), None)
            note = f"renamed / moved from `{cand}`" if cand else ""
        else:
            note = f"signature changed: `{_sig(left_old[cand])}` → `{_sig(ni)}`"
        if cand is not None:
            out["members"].append(MemberChange(nid, "modified", left_old.pop(cand), ni, note))
            del left_new[nid]
    for nid, ni in left_new.items():
        out["members"].append(MemberChange(nid, "added", None, ni))
    for oid, oi in left_old.items():
        out["members"].append(MemberChange(oid, "removed", oi, None))
    # statement-level union trees, call changes, significance
    for ch in out["members"]:
        o_st = (ch.old.member.stmts or []) if ch.old else []
        n_st = (ch.new.member.stmts or []) if ch.new else []
        if ch.status == "modified":
            ch.stmts = align(o_st, n_st)
        elif ch.status == "added":
            ch.stmts = _clone(n_st)
            _mark(ch.stmts, "added")
        else:
            ch.stmts = _clone(o_st)
            _mark(ch.stmts, "removed")
        ch.counts = _changes(ch.stmts)
        oc = {t for k, t in (ch.old.uses if ch.old else []) if k in CALLS}
        nc = {t for k, t in (ch.new.uses if ch.new else []) if k in CALLS}
        ch.calls_added, ch.calls_removed = sorted(nc - oc), sorted(oc - nc)
        mi = ch.new or ch.old
        public_api = "public" in mi.member.mods and not mi.unity_event
        empty = not any(True for _ in _all(ch.stmts))
        if ch.note.startswith("signature") or (ch.status == "removed" and public_api and not empty):
            ch.rank = "crucial"  # callers may break
        elif empty and ch.status in ("added", "removed"):
            ch.rank = "low"  # an empty method (e.g. a Unity stub) came or went
        elif ch.status != "modified" or ch.counts["cond"] or ch.counts["branch"] or ch.calls_added or ch.calls_removed:
            ch.rank = "medium"  # new entry points / methods, changed conditions, branches or calls
        elif not any(ch.counts[k] for k in ("added", "removed", "modified")):
            ch.rank = "none"
        else:
            ch.rank = "low"
    # fields / properties / constants
    for t in new.project.types.values():
        ot = old.project.types.get(t.full)
        nf = {m.name: m for m in t.members if m.kind in ("field", "const", "property", "event", "enum_member")}
        of = {m.name: m for m in (ot.members if ot else []) if m.kind in ("field", "const", "property", "event", "enum_member")}
        for name, m in nf.items():
            o = of.get(name)
            if o is None:
                out["fields"].append((t.full, name, "added", m, None))
            elif o.fingerprint != m.fingerprint:
                out["fields"].append((t.full, name, "modified", m, o))
        for name, o in of.items():
            if name not in nf:
                out["fields"].append((t.full, name, "removed", None, o))
    for t in old.project.types.values():
        if t.full not in new.project.types:
            for m in t.members:
                if m.kind in ("field", "const", "property", "event", "enum_member"):
                    out["fields"].append((t.full, m.name, "removed", None, m))
    # files
    for path in sorted(set(old.files) | set(new.files)):
        o, n = old.files.get(path), new.files.get(path)
        if o is None:
            out["files"].append((path, "added", n))
        elif n is None:
            out["files"].append((path, "removed", o))
        elif o.fingerprint != n.fingerprint:
            out["files"].append((path, "modified", n))
        elif old.sources[path] != new.sources[path]:
            out["files"].append((path, "comments / format only", n))
    return out


# ----------------------------------------------------------------------------- report

def _file_of(model_new: Model, model_old: Model, mid_or_type: str) -> str:
    for m in (model_new, model_old):
        mi = m.members.get(mid_or_type)
        if mi is not None:
            return mi.type.file
        t = m.project.types.get(mid_or_type)
        if t is not None:
            return t.file
    return ""


def _is_external(model: Model, path: str) -> bool:
    return model.project.path_is_external(path)


def clusters(d: dict, old: Model, new: Model) -> list:
    """Changes that touch each other (calls / shared fields) form one cluster: check each against the commit message."""
    nodes = [c.id for c in d["members"]] + [f"{t}.{n}" for t, n, *_ in d["fields"]]
    parent = {n: n for n in nodes}

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    def union(a, b):
        if a in parent and b in parent:
            parent[find(a)] = find(b)

    for c in d["members"]:
        for mi in (c.old, c.new):
            if mi is None:
                continue
            for k, t in mi.uses:
                union(c.id, t)
            union(c.id, mi.type.full + "." + mi.member.name)
    groups: dict[str, list] = {}
    for n in nodes:
        groups.setdefault(find(n), []).append(n)
    return sorted(groups.values(), key=lambda g: (-len(g), g[0]))


def impact_map(d: dict, old: Model, new: Model) -> str:
    """Changed members + one hop of context. Callers of changed code that did not change are 'impacted'."""
    mb = MapBuilder("LR", diff=True)
    changed = {c.id: c for c in d["members"]}
    fields = {f"{t}.{n}": st for t, n, st, *_ in d["fields"]}

    def label(mid, model):
        mi = model.members.get(mid)
        if mi is not None:
            return esc(f"{icon(mi, set()).strip()} {mi.type.name} · {short(mid)}".strip())
        return esc(mid)

    for mid, c in changed.items():
        mi = c.new or c.old
        ext = _is_external(new, mi.type.file)
        mb.node(mid_node(mid), ("" if c.status == "modified" else {"added": "+ ", "removed": "− "}[c.status])
                + label(mid, new if c.new else old) + ("  · AI/" if ext else ""), "own", status=c.status)
    for f, st in fields.items():
        mb.node(mid_node(f), esc(("+ " if st == "added" else "− " if st == "removed" else "~ ") + f.replace(".", " · ", 1)),
                "other", shape="round", status=st)
    for mid, c in changed.items():
        for model, side in ((c.new, "new"), (c.old, "old")):
            if model is None:
                continue
            for k, t in model.uses:
                # only what changed: calls between changed functions, and changed fields they set (no reads,
                # no unchanged context: that is what made the map busy)
                if not (k in CALLS or (k == "set" and t in fields)):
                    continue
                if t not in changed and t not in fields:
                    continue
                in_old = c.old is not None and (k, t) in c.old.uses
                in_new = c.new is not None and (k, t) in c.new.uses
                st = "same" if in_old and in_new else ("added" if in_new else "removed")
                if side == "old" and in_new:
                    continue
                style = "call" if k in CALLS else ("write" if k == "set" else "data")
                lab = "" if k in CALLS else ("sets" if k == "set" else "reads")
                mb.edge(mid_node(mid), mid_node(t), lab, style, st)
        # callers that didn't change: they may behave differently now
        if c.status != "removed":
            for caller in new.callers.get(mid, [])[:4]:
                if caller in changed:
                    continue
                mb.node(mid_node(caller), label(caller, new), "own", status="impacted")
                mb.edge(mid_node(caller), mid_node(mid), "", "call", "same")
        mi = c.new or c.old
        if mi.unity_event:
            from .mermaid import UNITY_PHASE
            ph = UNITY_PHASE.get(mi.member.name, "callback")
            u = mb.node("U_" + ph.replace(" ", "_").replace("-", "_"), f"Unity · {ph}", "engine", shape="stadium")
            mb.edge(u, mid_node(mid), "", "engine", "added" if c.status == "added" else
                    "removed" if c.status == "removed" else "same")
    linked = {e[0] for e in mb.edges} | {e[1] for e in mb.edges}
    for f in fields:  # a field nothing sets here is already in the list above: keep the map small
        if mid_node(f) not in linked:
            mb.nodes.pop(mid_node(f), None)
    return mb.render()


def _what(c) -> str:
    """A few words for the summary list."""
    n_st = sum(1 for _ in _all(c.stmts))
    if c.status == "added":
        return "new" if n_st else "new, empty"
    if c.status == "removed":
        return "removed" if n_st else "removed (was empty)"
    bits = []
    cn = c.counts
    for k, w in (("added", "added"), ("removed", "removed"), ("modified", "changed")):
        if cn[k]:
            bits.append(f"{cn[k]} {w}")
    if c.note:
        bits.append(c.note)
    return ", ".join(bits) or "reformatted"


WORD = {"added": "new", "removed": "removed", "modified": "changed"}
DOT = {"added": "🟢", "removed": "🔴", "modified": "🟡"}


def _chart(mi, stmts, cls, name, labels: dict, story_steps: dict, part: str):
    """One side of a function's diff. Before: removed steps red, changed steps yellow (old words). Now: new steps
    green, changed steps yellow (new words). Unchanged steps gray with their usual words. A changed box shows the
    AI's words for it (from the change's story), else its actual code, so the two sides show real values."""
    flow = build_flow(mi.title, stmts, "start", cls, name, ghosts=False)
    for n in flow.nodes:
        if n.kind in ("start", "end", "join"):
            continue
        if n.status == "same":
            n.label = labels.get(n.id, "")
        else:
            # Now side: the box is current code, so its usual AI words apply; Before side: the story's words, else code
            n.label = story_steps.get(n.id) or (labels.get(n.id, "") if part == "after" else "") or _code_label(n)
    flow.nodes[0].label = icon(mi, set()) + mi.member.name
    return flow, flow_to_mermaid(flow, unity_event=mi.member.name if mi.unity_event else None, coroutine=mi.coroutine,
                                 diff=part, short_name=name)


def _code_label(n) -> str:
    """The changed lines themselves, short (used when the AI hasn't put words on a changed box)."""
    lines = [l.text for l in n.lines if l.status != "same"] or [l.text for l in n.lines]
    text = " · ".join(re.sub(r"\s+", " ", t) for t in lines)
    if n.kind in ("decision", "case"):
        text += " ?"
    return text if len(text) <= 70 else text[:69].rstrip() + "…"


def _rows(stmts, out):
    """Changed steps as (before, now) code pairs; an added / removed if or loop is one row."""
    for st in stmts:
        full = re.sub(r"\s+", " ", st.full or st.text)
        if st.status == "added":
            out.append(("", full))
        elif st.status == "removed":
            out.append((full, ""))
        else:
            if st.status == "modified":
                old = re.sub(r"\s+", " ", getattr(st, "old_full", None) or st.old_text or "")
                out.append((old if st.kind == "simple" else (st.old_text or ""), full if st.kind == "simple" else st.text))
            for _, sub in st.parts:
                _rows(sub, out)
    return out


def _cut(t: str, n: int = 90) -> str:
    return t if len(t) <= n else t[: n - 1].rstrip() + "…"


def report(old: Model, new: Model, base: str, head: str, subject: str = "", notes: dict | None = None,
           story: dict | None = None) -> str:
    """story: the AI's words for this change (diffs/<commit>.yml): summary, parts, checks, and per function
    when / before / now / so (+ tag) and optional words for changed boxes (steps)."""
    notes = notes or {}
    story = story or {}
    said = story.get("changes", {}) or {}
    d = diff_models(old, new)
    own_all = [c for c in d["members"] if not _is_external(new, _file_of(new, old, c.id))]
    ext = [c for c in d["members"] if _is_external(new, _file_of(new, old, c.id))]
    own = [c for c in own_all if any(True for _ in _all(c.stmts)) or c.status == "modified"]
    quiet = [c for c in own_all if c not in own]  # empty methods that came or went: no behavior
    fields = [f for f in d["fields"] if not _is_external(new, _file_of(new, old, f[0]))]
    by_id = {c.id: c for c in own}
    # story order: the AI's parts (set-up before use, then along the calls), else the change's own clusters
    parts = story.get("parts") or []
    ordered, seen = [], set()
    for p_ in parts:
        for fid in p_.get("functions", []) or []:
            if fid in by_id and fid not in seen:
                ordered.append(by_id[fid])
                seen.add(fid)
    rest = sorted([c for c in own if c.id not in seen], key=lambda c: (RANK[c.rank], c.id))
    ordered += rest

    L = [GENERATED, "# What this change does", "", f"`{base}` → `{head}`" + (f" · *{subject}*" if subject else ""), ""]
    summary = story.get("summary") or []
    if isinstance(summary, str):
        summary = [summary]
    if summary:
        L += ["**In the game:**", ""] + [f"- {x}" for x in summary] + [""]
    else:
        L += ["*(No description yet: run `codegraph diff`, then write the .yml it names.)*", ""]
    counts = []
    for st_, word in (("modified", "changed"), ("added", "new"), ("removed", "removed")):
        n = sum(1 for c in own if c.status == st_)
        if n:
            counts.append(f"{DOT[st_]} {n} {word}")
    if fields:
        counts.append(f"{len(fields)} values")
    if quiet:
        counts.append(f"{len(quiet)} empty functions came or went")
    L += [" · ".join(counts), ""]
    if parts:
        for p_ in parts:
            items = [by_id[f] for f in (p_.get("functions") or []) if f in by_id]
            if not items:
                continue
            L.append(f"- **{p_.get('name', 'Part')}:** " + " → ".join(f"[`{_short(c.id)}`](#{_slug(c.id)})" for c in items))
        L.append("")
    else:
        L += ["| | Function | So |", "|---|---|---|"]
        for c in ordered:
            w = said.get(c.id, {}) or {}
            L.append(f"| {DOT[c.status]}&nbsp;{WORD[c.status]} | [`{c.id}`](#{_slug(c.id)}) | {esc_md(w.get('so') or '')} |")
        L.append("")
    checks = story.get("checks") or []
    if checks:
        L += ["**Check these:**", ""] + [f"- {x}" for x in checks] + [""]

    cls = Classifier(old.complex | new.complex, NOTABLE, old.complex_body | new.complex_body)
    for c in ordered:
        mi = c.new or c.old
        name = display_name(new if c.new else old, mi.type.full)
        note = notes.get(c.id)
        labels = note.steps if note is not None else {}
        before, after = split_union(c.stmts, cls)
        w = said.get(c.id, {}) or {}
        story_steps = {str(k): str(v) for k, v in (w.get("steps") or {}).items()}
        L += [f'<a id="{_slug(c.id)}"></a>', "", f"## {DOT[c.status]} {c.id} · {WORD[c.status]}", ""]
        card = []
        for key, word in (("when", "When"), ("before", "Before"), ("now", "Now"), ("so", "So")):
            if w.get(key):
                tag = f" *[{w['tag']}]*" if key == "so" and w.get("tag") else ""
                card.append(f"**{word}:** {w[key]}{tag}")
        if card:
            L += ["> " + "<br>".join(card), ""]
        if c.status == "modified":
            rows = _rows(c.stmts, [])
            same = sum(1 for x in _all(c.stmts) if x.status == "same" and x.kind not in ("break", "continue"))
            if rows:
                L += ["| Before | Now |", "|---|---|"]
                for b, a in rows:
                    L.append(f"| {('<code>' + esc_md(_cut(b)) + '</code>') if b else '—'} | "
                             f"{('<code>' + esc_md(_cut(a)) + '</code>') if a else '—'} |")
                L += ["", f"<sub>{same} unchanged steps not listed</sub>", ""]
        if c.status != "added":
            _, chart = _chart(c.old, before, cls, name, labels, story_steps, "before")
            L += ["**Before**" if c.status != "removed" else "**What it did**", "", "```mermaid", chart, "```", ""]
        if c.status != "removed":
            _, chart = _chart(c.new, after, cls, name, labels, story_steps, "after")
            L += ["**Now**", "", "```mermaid", chart, "```", ""]
    L += ["<details><summary>How the changed pieces connect · other changes</summary>", "", "```mermaid",
          impact_map({"members": own, "fields": fields}, old, new), "```", ""]
    if fields:
        L.append("Values: " + ", ".join(DOT[st_] + f" `{t.split('.')[-1]}.{n}`" for t, n, st_, m, o in fields))
        warn = [f"`{n}` {o.init or '(none)'} → {m.init or '(none)'}" for t, n, st_, m, o in fields
                if st_ == "modified" and o.init != m.init and m.kind == "field" and ("public" in m.mods or "SerializeField" in m.attrs)]
        if warn:
            L.append("<br>⚠️ New defaults don't reach objects already saved in scenes: " + ", ".join(warn))
        L.append("")
    if quiet:
        L += ["Empty functions: " + ", ".join(f"{DOT[c.status]} `{c.id}` {WORD[c.status]}" for c in quiet), ""]
    if ext:
        L += [f"`AI/` (outside library): {len(ext)} functions changed inside.", ""]
    L += ["</details>", ""]
    return "\n".join(L) + "\n"


def _short(mid: str) -> str:
    parts = mid.split("(")[0].split(".")
    return ".".join(parts[-2:])


def _slug(mid: str) -> str:
    return "d-" + "".join(ch if ch.isalnum() else "-" for ch in mid).strip("-").lower()


def todo(old: Model, new: Model, story: dict, notes: dict | None = None) -> list:
    """Changed functions the AI hasn't described yet."""
    notes = notes or {}
    d = diff_models(old, new)
    said = story.get("changes", {}) or {}
    out = []
    for c in d["members"]:
        if _is_external(new, _file_of(new, old, c.id)) or not (any(True for _ in _all(c.stmts)) or c.status == "modified"):
            continue
        w = said.get(c.id) or {}
        need = [k for k in (("when", "now", "so") if c.status == "added" else ("before",) if c.status == "removed"
                            else ("when", "before", "now", "so")) if not w.get(k)]
        if need:
            out.append(f"{c.id} ({WORD[c.status]}): {', '.join(need)}")
        if c.status == "modified":  # changed boxes: a few words each, on each side (else the chart shows code)
            cls = Classifier(old.complex | new.complex, NOTABLE, old.complex_body | new.complex_body)
            before, after = split_union(c.stmts, cls)
            have = {str(k) for k in (w.get("steps") or {})}
            for side, tree, mi in (("before", before, c.old), ("now", after, c.new)):
                f = build_flow(mi.title, tree, "start", cls, ghosts=False)
                for n in f.nodes:
                    current = side == "now" and n.id in (notes.get(c.id).steps if notes.get(c.id) else {})
                    if n.status != "same" and n.kind not in ("start", "end", "join", "stub") and n.id not in have \
                            and not current:
                        code = " | ".join(l.text for l in n.lines if l.status != "same")
                        out.append(f"  steps {n.id} ({side}): {code[:140]}")
    head = []
    if not story.get("summary"):
        head.append("summary: 1-3 'before -> now' lines, what changes in the game, tagged [intended] / [side effect?]")
    if not story.get("parts"):
        head.append("parts: name each part of the change, functions in story order (set-up before use)")
    out = head + out
    return out


def short_call(mid: str) -> str:
    parts = mid.split("(")[0].split(".")
    return ".".join(parts[-2:]) if len(parts) >= 2 else mid


def cmd_diff(a, repo: str, home: str):
    cfg = load_config(home)
    old = build_model(load_sources(repo, cfg, a.base), cfg)
    new = build_model(load_sources(repo, cfg, a.head), cfg)
    subject = ""
    if a.head:
        try:
            subject = subprocess.run(["git", "-C", repo, "log", "-1", "--format=%s", a.head], check=True,
                                     capture_output=True, text=True).stdout.strip()
        except Exception:
            pass
    notes = {}
    for path in new.files:
        for n in load_notes(os.path.join(home, "notes"), path):
            notes[n.target] = n
    # the AI's words for this change live next to the report: diffs/<commit>.yml
    key = a.head and subprocess.run(["git", "-C", repo, "rev-parse", "--short", a.head], capture_output=True,
                                    text=True).stdout.strip() or "worktree"
    story_path = a.story or os.path.join(home, "diffs", key + ".yml")
    story = {}
    if os.path.exists(story_path):
        import yaml
        with open(story_path, encoding="utf8") as f:
            story = yaml.safe_load(f) or {}
    missing = todo(old, new, story, notes)
    if missing:
        import sys
        print(f"AI to-do: describe this change in {os.path.relpath(story_path, repo)}:", file=sys.stderr)
        for m in missing:
            print("  " + m, file=sys.stderr)
    text = report(old, new, a.base, a.head or "working tree", subject, notes, story)
    if a.out:
        os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
        with open(a.out, "w", encoding="utf8", newline="\n") as f:
            f.write(text)
        print("wrote", a.out)
    else:
        print(text)
