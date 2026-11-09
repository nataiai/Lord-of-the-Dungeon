"""Source files (from disk or a git revision) -> the full graph model."""
from __future__ import annotations

import fnmatch
import json
import os
import subprocess
from dataclasses import dataclass, field

from .csparse import FileInfo, Member, TypeDecl, parse_file
from .flow import Flow, build_flow
from .resolve import UNITY_EVENTS, Project, Resolver

DEFAULT_CONFIG = {
    "external": ["AI/*"],
    "ignore": ["CodeGraph~/*"],
}


def load_config(tool_root: str) -> dict:
    path = os.path.join(tool_root, "codegraph.json")
    cfg = dict(DEFAULT_CONFIG)
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            cfg.update(json.load(f))
    return cfg


def _ignored(path: str, cfg: dict) -> bool:
    return any(fnmatch.fnmatch(path, g) for g in cfg.get("ignore", []))


def load_sources(repo: str, cfg: dict, rev: str | None = None) -> dict:
    """{path: bytes} of every .cs file, from the working tree (rev=None) or a git revision."""
    out = {}
    if rev is None:
        for dirpath, dirnames, filenames in os.walk(repo):
            dirnames[:] = sorted(d for d in dirnames if not d.startswith(".") and not d.endswith("~"))
            for fn in sorted(filenames):
                if fn.endswith(".cs"):
                    full = os.path.join(dirpath, fn)
                    rel = os.path.relpath(full, repo).replace(os.sep, "/")
                    if not _ignored(rel, cfg):
                        with open(full, "rb") as f:
                            out[rel] = f.read()
        return out
    names = subprocess.run(["git", "-C", repo, "ls-tree", "-r", "--name-only", rev], check=True,
                           capture_output=True, text=True).stdout.splitlines()
    for rel in sorted(names):
        if rel.endswith(".cs") and not _ignored(rel, cfg) and not any(p.endswith("~") for p in rel.split("/")[:-1]):
            out[rel] = subprocess.run(["git", "-C", repo, "show", f"{rev}:{rel}"], check=True, capture_output=True).stdout
    return out


@dataclass
class MemberInfo:
    id: str
    member: Member
    type: TypeDecl
    title: str  # e.g. "⚡ Update()"
    unity_event: bool = False
    coroutine: bool = False
    flow: Flow | None = None
    uses: list = field(default_factory=list)  # [(kind, target id)]
    overrides: str | None = None  # id of the base method this one overrides


@dataclass
class Model:
    files: dict  # path -> FileInfo
    project: Project
    members: dict  # id -> MemberInfo (methods, ctors, accessors with bodies)
    callers: dict  # target id -> sorted [member ids]
    type_deps: dict  # (from type, to type) -> set(kinds)
    sources: dict  # path -> bytes

    def file_members(self, path: str) -> list:
        return [mi for mi in self.members.values() if mi.type.file == path]


def member_title(m: Member, unity: bool, coroutine: bool) -> str:
    icon = "⚡ " if unity else ("⏳ " if coroutine else "")
    if m.kind == "accessor":
        prop, kw = m.name.rsplit(".", 1)
        return f"{prop} · {kw}"
    params = ", ".join((p.mod + " " if p.mod else "") + p.type + " " + p.name for p in m.params)
    return f"{icon}{m.name}({params})"


def build_model(sources: dict, cfg: dict) -> Model:
    files = []
    for path, data in sorted(sources.items()):
        files.append(parse_file(path, data))
    project = Project(files, cfg.get("external", []))
    res = Resolver(project)
    members: dict[str, MemberInfo] = {}
    for fi in files:
        src = fi._src
        for t in fi.types:
            unity_type = project.is_unity_component(t)
            for m in t.members:
                if m.kind in ("method", "ctor", "property"):
                    res.annotate_member(t, m, src)
                if m.kind in ("method", "ctor"):
                    mid = project.mid(m)
                    unity = unity_type and m.name in UNITY_EVENTS
                    co = m.type.strip() in ("IEnumerator", "System.Collections.IEnumerator", "IEnumerable")
                    mi = MemberInfo(mid, m, t, member_title(m, unity, co), unity, co)
                    members[mid] = mi
                for k, a in m.accessors.items():
                    if a.stmts:
                        aid = project.mid(a)
                        members[aid] = MemberInfo(aid, a, t, member_title(a, False, False))

    # accessors that run project code count as calls wherever the property is read / written
    # (fixpoint: an accessor that reads such a property runs project code too)
    logic_acc: set = set()
    accs = {mid: mi for mid, mi in members.items() if mi.member.kind == "accessor"}
    changed = True
    while changed:
        changed = False
        for mid, mi in accs.items():
            if mid in logic_acc:
                continue
            for k, target in _all_uses(mi.member.stmts):
                if (k == "call" and target in members and members[target].member.kind != "accessor") or (
                        k in ("get", "set") and f"{target}.{k}" in logic_acc):
                    logic_acc.add(mid)
                    changed = True
                    break
    for mid, mi in members.items():
        uses = []
        for k, target in _all_uses(mi.member.stmts):
            if (k, target) not in uses:
                uses.append((k, target))
            if k in ("get", "set"):
                acc = f"{target}.{k}"
                if acc in logic_acc and acc != mid and ("call", acc) not in uses:
                    uses.append(("call", acc))
        mi.uses = uses
        start_kind = "start"
        mi.flow = build_flow(mi.title, _with_accessor_calls(mi.member.stmts or [], logic_acc, mid), start_kind)

    callers: dict[str, list] = {}
    for mid, mi in members.items():
        for k, target in mi.uses:
            if k in ("call", "base", "ref", "new"):
                callers.setdefault(target, [])
                if mid not in callers[target]:
                    callers[target].append(mid)
    # virtual dispatch: a call to Character.recieveDamage may run Player/Enemy/Chest.recieveDamage
    for mid, mi in members.items():
        m = mi.member
        if "override" not in m.mods:
            continue
        for base in project.base_chain(mi.type)[1:]:
            bm = next((x for x in base.members if x.name == m.name and x.kind == m.kind
                       and len(x.params) == len(m.params)), None)
            if bm is not None:
                bid = project.mid(bm)
                mi.overrides = bid
                for c in callers.get(bid, []):
                    # only calls that dispatch: `base.X()` runs the base version, never this override
                    if ("call", bid) in members[c].uses or ("ref", bid) in members[c].uses:
                        callers.setdefault(mid, [])
                        if c not in callers[mid] and c != mid:
                            callers[mid].append(c)
                break
    for v in callers.values():
        v.sort()

    # class-level dependencies (for the project map)
    deps: dict[tuple, set] = {}

    def dep(a: str, b: str, kind: str):
        if a != b and b in project.types and a in project.types:
            deps.setdefault((a, b), set()).add(kind)

    for t in project.types.values():
        for b in t.bases:
            bt = project.find_type(b, t)
            if bt is not None:
                dep(t.full, bt.full, "inherits")
        for m in t.members:
            if m.kind in ("field", "property"):
                ft = project.find_type(_core_type(m.type), t)
                if ft is not None and ft.kind != "enum":
                    dep(t.full, ft.full, "has")
    for mid, mi in members.items():
        owner = mi.type.full
        for k, target in mi.uses:
            if k == "new":
                dep(owner, target, "creates")
                continue
            tm = project.member_by_id.get(target)
            if tm is None:
                continue
            kind = {"call": "calls", "base": "calls", "ref": "calls", "set": "writes", "get": "reads"}.get(k, "uses")
            if tm.kind == "enum_member":
                kind = "reads"
            dep(owner, tm.owner, kind)
    return Model({f.path: f for f in files}, project, members, callers, deps, sources)


def _core_type(t: str) -> str:
    t = t.strip().rstrip("?")
    while t.endswith("[]"):
        t = t[:-2]
    if "<" in t:
        inner = t[t.index("<") + 1 : -1]
        return inner.split(",")[-1].strip()
    return t


def _all_uses(stmts):
    for s in stmts or []:
        for u in s.uses:
            yield u
        for _, sub in s.parts:
            yield from _all_uses(sub)


def _with_accessor_calls(stmts, logic_acc: set, self_id: str):
    for s in stmts:
        for k, target in list(s.uses):
            acc = f"{target}.{k}"
            if k in ("get", "set") and acc in logic_acc and acc != self_id and ("call", acc) not in s.uses:
                s.uses.append(("call", acc))
        for _, sub in s.parts:
            _with_accessor_calls(sub, logic_acc, self_id)
    return stmts
