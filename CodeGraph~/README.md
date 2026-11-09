# CodeGraph

Browse: [graph/index.md](graph/index.md). How to read it: [DESIGN.md](DESIGN.md).

## For the AI, after changing code

1. `python "CodeGraph~/tool/codegraph.py" notes` — write what it lists in `CodeGraph~/notes/<file>.yml`
   (see `notes/Enemy.cs.yml`): a few plain words per box, no function names, decisions as short questions.
2. `python "CodeGraph~/tool/codegraph.py" diff HEAD~1 HEAD --out "CodeGraph~/diffs/<commit>.md"` — it lists the
   functions to describe in `CodeGraph~/diffs/<commit>.yml` (see `diffs/b47c315.yml`): `before`, `now`, `so`,
   one short line each, plus a 1-3 line `summary`.
3. `python "CodeGraph~/tool/codegraph.py" build`, then commit everything with the code.
