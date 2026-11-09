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
import subprocess

from .csparse import Stmt
from .flow import build_flow
from .mermaid import MapBuilder, esc, flow_to_mermaid, mid_node, short
from .model import Model, build_model, load_config, load_sources
from .pages import CALLS, GENERATED, LEGEND, icon, per_frame

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
                if k not in CALLS and not (k in ("get", "set") and t in fields):
                    continue
                if t not in changed and t not in fields:
                    if k in CALLS and side == "new":
                        tm = new.members.get(t)
                        if tm is not None:
                            mb.node(mid_node(t), label(t, new), "own")
                    else:
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
    return mb.render()


def report(old: Model, new: Model, base: str, head: str, subject: str = "") -> str:
    d = diff_models(old, new)
    frame = per_frame(new)
    own = [c for c in d["members"] if not _is_external(new, _file_of(new, old, c.id))]
    ext = [c for c in d["members"] if _is_external(new, _file_of(new, old, c.id))]
    own.sort(key=lambda c: (RANK[c.rank], c.id))
    L = [GENERATED, f"# Graph diff · `{base}` → `{head}`", ""]
    if subject:
        L += [f"> {subject}", ""]
    files = d["files"]
    L.append(f"{len(files)} files · {len(own)} methods changed in your code"
             + (f" · {len(ext)} in `AI/` (external, API only below)" if ext else "")
             + f" · {len(d['fields'])} state members changed")
    L.append("")
    L.append("<sub>**Colors:** blue `+` added · vermilion dashed `−` removed · yellow `~` changed (with *was:*) · "
             "gray = unchanged context · dotted outline = not changed but calls changed code. "
             f"Ranks: {RANK_ICON['crucial']} signature / public API · {RANK_ICON['medium']} conditions, branches or "
             f"calls · {RANK_ICON['low']} plain statements · {RANK_ICON['none']} comments / format. {LEGEND}</sub>")
    L.append("")
    # ---- text list first (also the fallback when diagrams don't render)
    L.append("## What changed")
    L.append("")
    for c in own:
        mi = c.new or c.old
        bits = []
        cn = c.counts
        n_st = sum(1 for _ in _all(c.stmts))
        if c.status == "added":
            bits.append(f"new, {n_st} statements" if n_st else "new, empty")
        elif c.status == "removed":
            bits.append(f"removed ({n_st} statements)" if n_st else "removed (it was empty)")
        else:
            for k, w in (("added", "added"), ("removed", "removed"), ("modified", "changed")):
                if cn[k]:
                    bits.append(f"{cn[k]} {w}")
            if cn["cond"]:
                bits.append(f"{cn['cond']} condition{'s' if cn['cond'] > 1 else ''} changed")
        if c.note:
            bits.append(c.note)
        if c.calls_added:
            bits.append("now calls " + ", ".join(f"`{t}`" for t in c.calls_added))
        if c.calls_removed:
            bits.append("no longer calls " + ", ".join(f"`{t}`" for t in c.calls_removed))
        if c.new is not None and (c.id in frame or c.new.unity_event and c.new.member.name in ("Update", "FixedUpdate", "LateUpdate")):
            bits.append("🔁 runs every frame")
        sym = {"added": "+", "removed": "−", "modified": "~"}[c.status]
        L.append(f"- {RANK_ICON[c.rank]} **{sym} `{c.id}`** {icon(mi, set()).strip()} — " + "; ".join(bits))
    for t, n, st, m, o in d["fields"]:
        if _is_external(new, _file_of(new, old, t)):
            continue
        sym = {"added": "+", "removed": "−", "modified": "~"}[st]
        desc = ""
        if st == "modified":
            desc = f"`{o.type} {o.name}{(' = ' + o.init) if o.init else ''}` → `{m.type} {m.name}{(' = ' + m.init) if m.init else ''}`"
            if o.init != m.init and m.kind == "field" and ("public" in m.mods or "SerializeField" in m.attrs):
                desc += " ⚠️ *default changed: objects already saved in scenes / prefabs keep their old value*"
        elif st == "added":
            desc = f"`{m.type} {m.name}{(' = ' + m.init) if m.init else ''}`" + (f" — {m.tooltip or m.comment}" if (m.tooltip or m.comment) else "")
        else:
            desc = f"`{o.type} {o.name}`"
        L.append(f"- {RANK_ICON['low']} **{sym} `{t}.{n}`** (state) — {desc}")
    for path, st, f in files:
        if st == "comments / format only":
            L.append(f"- {RANK_ICON['none']} `{path}` — comments / formatting only")
    L.append("")
    # ---- clusters
    cl = [g for g in clusters(d, old, new) if any(not _is_external(new, _file_of(new, old, x)) for x in g)]
    if len(cl) > 1:
        L.append("## Separate parts of this change")
        L.append("")
        L.append("Changes that don't call or share state with each other. Check each part against the commit message:")
        L.append("")
        for i, g in enumerate(cl, 1):
            L.append(f"{i}. " + ", ".join(f"`{x}`" for x in g))
        L.append("")
    # ---- impact map
    L.append("## Impact map")
    L.append("")
    L.append("Changed methods and state, with one step of context. Edges: calls (solid), sets (dashed), reads (dotted).")
    L.append("")
    L.append("```mermaid")
    L.append(impact_map({"members": own, "fields": [f for f in d["fields"] if not _is_external(new, _file_of(new, old, f[0]))]}, old, new))
    L.append("```")
    L.append("")
    # ---- per method union flowcharts
    L.append("## Changed methods")
    L.append("")
    for c in own:
        if c.status == "removed" and not any(True for _ in _all(c.stmts)):
            continue
        mi = c.new or c.old
        sym = {"added": "+", "removed": "−", "modified": "~"}[c.status]
        L.append(f"### {RANK_ICON[c.rank]} {sym} {icon(mi, frame)}{c.id}")
        L.append("")
        meta = []
        if c.new is not None:
            meta.append(f"now L{c.new.member.line}–{c.new.member.end}")
        if c.old is not None:
            meta.append(f"was L{c.old.member.line}–{c.old.member.end}")
        if c.note:
            meta.append(c.note)
        L.append(" · ".join(meta))
        L.append("")
        flow = build_flow(mi.title, c.stmts)
        L.append("```mermaid")
        L.append(flow_to_mermaid(flow, unity_event=mi.member.name if mi.unity_event else None, coroutine=mi.coroutine,
                                 diff=True))
        L.append("```")
        L.append("")
    # ---- external API
    if ext:
        L.append("## `AI/` (external): API changes only")
        L.append("")
        for c in sorted(ext, key=lambda c: c.id):
            mi = c.new or c.old
            if not ("public" in mi.member.mods or "internal" in mi.member.mods):
                continue
            if c.status == "modified" and not c.note:
                continue
            sym = {"added": "+", "removed": "−", "modified": "~"}[c.status]
            L.append(f"- **{sym} `{c.id}`** {c.note}")
        hidden = sum(1 for c in ext if c.status == "modified" and not c.note)
        if hidden:
            L.append(f"- *{hidden} method bodies changed inside `AI/` (not shown: external)*")
        L.append("")
    return "\n".join(L) + "\n"


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
    text = report(old, new, a.base, a.head or "working tree", subject)
    if a.out:
        os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
        with open(a.out, "w", encoding="utf8", newline="\n") as f:
            f.write(text)
        print("wrote", a.out)
    else:
        print(text)
