"""codegraph build | diff | notes | check  (run: python CodeGraph~/tool/codegraph.py <command> --help)"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys

from .csparse import short_hash
from .model import build_model, load_config, load_sources
from .notes import load_notes, resolve_notes
from .pages import file_page, index_page, page_path, per_frame

TOOL = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # CodeGraph~/tool
HOME = os.path.dirname(TOOL)  # CodeGraph~
NOTES = os.path.join(HOME, "notes")


def repo_root() -> str:
    try:
        return subprocess.run(["git", "-C", HOME, "rev-parse", "--show-toplevel"], check=True, capture_output=True,
                              text=True).stdout.strip()
    except Exception:
        return os.path.dirname(HOME)


def fingerprints(model) -> dict:
    """node id -> code hash, for every id a note can point at."""
    fp = {}
    for t in model.project.types.values():
        fp[t.full] = t.fingerprint
    for mid, m in model.project.member_by_id.items():
        fp[mid] = m.fingerprint
    for mid, mi in model.members.items():
        for n in mi.flow.nodes:
            fp[f"{mid}#{n.id}"] = short_hash(n.kind + "|" + n.text)
    return fp


def all_notes(model) -> dict:
    fp = fingerprints(model)
    known = set(fp)
    out = {}
    for path in model.files:
        ns = load_notes(NOTES, path)
        if ns:
            out[path] = resolve_notes(ns, fp, known)
    return out


def coverage(model) -> tuple:
    """(statements held by flowchart boxes, statements in the code) for your own (non-external) files."""
    from .flow import count_statements, covered
    total = shown = 0
    for mi in model.members.values():
        if model.files[mi.type.file].external:
            continue
        total += count_statements(mi.member.stmts or [])
        shown += covered(mi.flow)
    return shown, total


def cmd_build(a):
    repo = repo_root()
    cfg = load_config(HOME)
    model = build_model(load_sources(repo, cfg, a.rev), cfg)
    out = os.path.abspath(a.out or os.path.join(HOME, "graph"))
    from . import pages
    pages.ROOT = os.path.relpath(repo, out).replace(os.sep, "/")
    notes = all_notes(model)
    frame = per_frame(model)
    every_note = {n.target: n for ns in notes.values() for n in ns}  # callees' notes, for inlined steps / purposes
    only = set(a.only or model.files)
    written = set()
    for path in sorted(model.files):
        if path not in only:
            continue
        page = page_path(path)
        dest = os.path.join(out, page)
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        notes_rel = os.path.relpath(NOTES, os.path.dirname(dest)).replace(os.sep, "/")
        with open(dest, "w", encoding="utf8", newline="\n") as f:
            f.write(file_page(model, path, page, notes.get(path, []), frame, notes_rel, every_note))
        written.add(path)
    cov = coverage(model)
    with open(os.path.join(out, "index.md"), "w", encoding="utf8", newline="\n") as f:
        f.write(index_page(model, "index.md", notes, frame, written, cov))
    print(f"wrote {len(written)} file pages + index.md to {os.path.relpath(out, repo)}  "
          f"(statements in flowcharts: {cov[0]}/{cov[1]})")
    if cov[0] != cov[1]:
        print("warning: some statements are not in any flowchart node", file=sys.stderr)


def cmd_notes(a):
    """The AI's to-do list: one-line notes to write, boxes without a short label (with their code), stale notes."""
    from .pages import is_chart
    repo = repo_root()
    cfg = load_config(HOME)
    model = build_model(load_sources(repo, cfg), cfg)
    notes = all_notes(model)
    for path in sorted(model.files):
        if a.files and path not in a.files:
            continue
        fi = model.files[path]
        if fi.external:
            continue
        have = {n.target: n for n in notes.get(path, [])}
        todo = []
        for t in fi.types:
            if t.outer is None and t.full not in have:
                todo.append(f"  note   {t.full}  (code {t.fingerprint}): one line, what it is")
        for mi in sorted(model.file_members(path), key=lambda m: m.member.line):
            n = have.get(mi.id)
            trivial = mi.member.kind == "accessor" and not any(k in ("call", "base") and t in model.complex
                                                                for k, t in mi.uses)
            if trivial:
                continue
            if n is None or not n.text:
                todo.append(f"  note   {mi.id}  (code {mi.member.fingerprint}): one line, what it does")
            elif n.status != "current":
                todo.append(f"  stale  {mi.id}  note {n.anchor.get('hash')} -> code {mi.member.fingerprint}")
            if not is_chart(mi):
                continue
            steps = n.steps if n is not None else {}
            ids = {x.id for x in mi.flow.nodes}
            for x in mi.flow.steps:
                if x.kind in ("case", "join") or x.id in steps:
                    continue
                code = " | ".join(l.text for l in x.lines)
                todo.append(f"  label  {mi.id} {x.id} [{x.kind}]  {code[:150]}")
            for sid in steps:
                if sid not in ids:
                    todo.append(f"  gone   {mi.id} {sid}  (label for code that changed: delete it)")
        if todo:
            print(path)
            print("\n".join(todo))


def cmd_dump(a):
    """Every function's note and every box: id, kind, the AI's words, and the exact code (for reviewing the words)."""
    repo = repo_root()
    cfg = load_config(HOME)
    model = build_model(load_sources(repo, cfg), cfg)
    notes = all_notes(model)
    for path in sorted(model.files):
        if a.files and path not in a.files:
            continue
        if model.files[path].external:
            continue
        have = {n.target: n for n in notes.get(path, [])}
        print(f"=== {path}")
        for t in model.files[path].types:
            n = have.get(t.full)
            print(f"[type] {t.full}: {n.text if n else '(no note)'}")
        for mi in sorted(model.file_members(path), key=lambda m: m.member.line):
            n = have.get(mi.id)
            print(f"\n[function] {mi.id}  L{mi.member.line}-{mi.member.end}: {n.text if n and n.text else '(no note)'}")
            steps = n.steps if n else {}
            for x in mi.flow.steps:
                if x.kind in ("case", "join"):
                    continue
                calls = (" calls " + ", ".join(x.calls)) if x.calls else ""
                print(f"   box {x.id} [{x.kind}]{calls}: {steps.get(x.id, '(no label)')!r}")
                for l in x.lines:
                    print(f"        L{l.line}: {l.text}")


def cmd_check(a):
    repo = repo_root()
    cfg = load_config(HOME)
    model = build_model(load_sources(repo, cfg), cfg)
    bad = 0
    for path, ns in all_notes(model).items():
        for n in ns:
            if n.status == "orphaned" or n.broken_mentions:
                print(f"{path}: {n.target}: {'orphaned' if n.status == 'orphaned' else 'broken mentions ' + str(n.broken_mentions)}")
                bad += 1
            elif n.status == "drifted":
                print(f"{path}: {n.target}: drifted (warning)")
    cov = coverage(model)
    if cov[0] != cov[1]:
        print(f"coverage: {cov[0]}/{cov[1]} statements in flowcharts")
        bad += 1
    sys.exit(1 if bad else 0)


def main(argv=None):
    ap = argparse.ArgumentParser(prog="codegraph", description=__doc__)
    sub = ap.add_subparsers(dest="cmd", required=True)
    b = sub.add_parser("build", help="write the graph pages")
    b.add_argument("--out", help="output folder (default CodeGraph~/graph)")
    b.add_argument("--only", nargs="*", help="only these source files (paths like Enemy.cs Map/Hole.cs)")
    b.add_argument("--rev", help="build from a git revision instead of the working tree")
    b.set_defaults(fn=cmd_build)
    d = sub.add_parser("diff", help="graph diff between two revisions")
    d.add_argument("base", nargs="?", default="HEAD", help="older revision (default HEAD)")
    d.add_argument("head", nargs="?", default=None, help="newer revision (default: the working tree)")
    d.add_argument("--out", help="write the report here (default: print)")
    d.add_argument("--story", help="the AI's words for this change (default: CodeGraph~/diffs/<commit>.yml)")
    d.set_defaults(fn=lambda a: __import__("codegraph.diff", fromlist=["cmd_diff"]).cmd_diff(a, repo_root(), HOME))
    n = sub.add_parser("notes", help="what needs a note / which notes drifted (the AI's to-do list)")
    n.add_argument("files", nargs="*")
    n.set_defaults(fn=cmd_notes)
    du = sub.add_parser("dump", help="every box with its AI words and its code (for reviewing the words)")
    du.add_argument("files", nargs="*")
    du.set_defaults(fn=cmd_dump)
    c = sub.add_parser("check", help="exit 1 on orphaned notes, broken mentions or unmapped statements")
    c.set_defaults(fn=cmd_check)
    a = ap.parse_args(argv)
    a.fn(a)


if __name__ == "__main__":
    main()
