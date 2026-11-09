# CodeGraph

Every C# function as a small chart. A program draws it; the AI writes the words.

## Reading a chart

- 🟢 **▶ green** — where it starts (and when Unity runs it)
- ⬜ **white** — simple math: setting values, small helpers
- 🔵 **blue** — goes into another function; the second line says what that function does.
  A short function is drawn **inside** the box group instead.
- 🟡 **yellow diamond** — a question: **yes** goes green, **no** goes red
- 🔁 loop · ⏳ wait · ✚ spawn · ✖ remove · 🔴 **⏹ stop**

The exact code is under **Code** below each chart.

## Comparing two commits

`python "CodeGraph~/tool/codegraph.py" diff A B` writes, for every changed function:

- **Before / Now / So** — what it did, what it does now, what that means (written by the AI)
- a **Before** chart (what goes away in red) and a **Now** chart (what is new in green), like GitHub's split view

The AI's words for a change live in `diffs/<commit>.yml`; `diff` prints what is still missing.

## Commands

```
pip install -r "CodeGraph~/tool/requirements.txt"
python "CodeGraph~/tool/codegraph.py" build       # all charts into CodeGraph~/graph
python "CodeGraph~/tool/codegraph.py" diff A B    # compare two commits
python "CodeGraph~/tool/codegraph.py" notes       # words the AI still has to write
```

`AI/` is treated as an outside library. Unity ignores this folder (its name ends in `~`).
