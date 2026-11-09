"""The AI / human layer: notes stored next to (never inside) the generated graph.

notes/<source path>.yml, e.g. notes/Enemy.cs.yml:

    Enemy.Update:                 # a node id from the graph pages (type, member, or member#flowchart-node)
      kind: summary               # summary | usage | rationale | warning
      text: Walks to the player around walls ...
      mentions: [PathFinding.Direction]       # ids the text relies on; `check` fails if one is missing
      image: {path: images/x.svg, alt: "..."} # optional
      by: ai                      # ai | human
      anchor: {hash: d1033f11, commit: b47c315}  # the code this note was written for
      review: {status: unreviewed}               # unreviewed | reviewed (+ hash: the code that was reviewed)
      steps:                      # short text for each flowchart box, by box id (`codegraph notes` lists them)
        n0f6e05: Become solid once free

The status is never stored: it is computed on every run (current / drifted / orphaned).
"""
from __future__ import annotations

import os
from dataclasses import dataclass, field

import yaml


@dataclass
class Note:
    target: str
    kind: str = "summary"
    text: str = ""
    mentions: list = field(default_factory=list)
    image: dict | None = None
    by: str = "ai"
    anchor: dict = field(default_factory=dict)
    review: dict = field(default_factory=dict)
    steps: dict = field(default_factory=dict)  # flowchart box id -> short AI label (ids are code hashes: never stale)
    file: str = ""
    # computed
    status: str = "current"  # current | drifted | orphaned
    current_hash: str = ""
    broken_mentions: list = field(default_factory=list)

    @property
    def reviewed(self) -> bool:
        return self.review.get("status") == "reviewed" and self.review.get("hash") in (None, self.current_hash)


def load_notes(notes_dir: str, source_path: str) -> list:
    path = os.path.join(notes_dir, source_path + ".yml")
    if not os.path.exists(path):
        return []
    with open(path, encoding="utf8") as f:
        data = yaml.safe_load(f) or {}
    out = []
    for target, rec in data.items():
        rec = rec or {}
        out.append(Note(target=str(target), kind=rec.get("kind", "summary"), text=str(rec.get("text", "")).strip(),
                        mentions=list(rec.get("mentions", []) or []), image=rec.get("image"), by=rec.get("by", "ai"),
                        anchor=_anchor(rec.get("anchor")), review=dict(rec.get("review", {}) or {}),
                        steps={str(k): str(v) for k, v in (rec.get("steps", {}) or {}).items()}, file=path))
    return out


def _anchor(a) -> dict:
    """`anchor: d1033f11` (the code hash) or `anchor: {hash: d1033f11, commit: abc1234}`."""
    if isinstance(a, dict):
        return dict(a)
    return {"hash": str(a)} if a else {}


def resolve_notes(notes: list, fingerprints: dict, known_ids: set) -> list:
    """Fill in the computed status of each note. fingerprints: node id -> current code hash."""
    for n in notes:
        cur = fingerprints.get(n.target)
        n.current_hash = cur or ""
        if cur is None:
            n.status = "orphaned"
        elif n.anchor.get("hash") and n.anchor.get("hash") != cur:
            n.status = "drifted"
        else:
            n.status = "current"
        n.broken_mentions = [m for m in n.mentions if m not in known_ids]
    return notes


def badge(n: Note) -> str:
    """A short marker: who wrote it, and whether the code changed since."""
    who = "🤖" if n.by == "ai" else "👤"
    if n.status == "drifted":
        return f"{who} ⚠️ *code changed since this note*"
    if n.status == "orphaned":
        return f"{who} ❓ *orphaned: its code is gone*"
    return who
