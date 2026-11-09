# CodeGraph

Every C# file as small pictures. A program draws them; the AI writes the short text inside.

## Reading a chart (one per function)

| Box | Means |
|---|---|
| 🟨 pill | where it starts, and when Unity runs it (⚡ every frame, before the first frame, …) |
| gray box | simple math: setting values, small helpers |
| **blue box** | a call to another function that does real work (`→ Died()`), or an engine action (`→ Destroy()`) |
| hexagon | a decision that matters (yes / no) |
| small pill | stop / return |

The text in each box is written by the AI in plain words. The exact code is under **Code** below each chart.
Every line of code is inside some box (the generator checks it).

## Where the text comes from

- The **generator** (`tool/`) reads the code and draws the boxes. Same code, same picture.
- The **AI** writes `notes/<file>.yml`: one line per function, a few words per box.
- Each box's words are tied to that box's exact code. If the code changes, the words disappear (the box shows a
  short code summary instead) until the AI rewrites them, so the text can't go stale.

## Compare two commits

`python "CodeGraph~/tool/codegraph.py" diff A B` writes the same charts with changes marked:
blue `+` added · orange `−` removed · yellow `~` changed, plus a short list of what changed.

## Commands

```
pip install -r "CodeGraph~/tool/requirements.txt"
python "CodeGraph~/tool/codegraph.py" build      # draw everything into CodeGraph~/graph
python "CodeGraph~/tool/codegraph.py" diff HEAD~1 HEAD
python "CodeGraph~/tool/codegraph.py" notes      # what the AI still has to write
python "CodeGraph~/tool/codegraph.py" check      # fails if a note points at code that is gone
```

`AI/` is treated as an outside library: it shows up only as the functions your code calls.
Unity ignores this folder (its name ends in `~`).
