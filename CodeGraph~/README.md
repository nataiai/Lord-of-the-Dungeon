# CodeGraph

Open [graph/index.md](graph/index.md) to browse every file as pictures. How to read them: [DESIGN.md](DESIGN.md).

## For the AI, after changing code

1. `python "CodeGraph~/tool/codegraph.py" notes` lists what is missing: one-line notes, and boxes without words.
2. Write them in `CodeGraph~/notes/<file>.yml` (see `notes/Enemy.cs.yml`): a few plain words per box, no function
   names (the chart adds them), decisions as short questions.
3. `python "CodeGraph~/tool/codegraph.py" build`, then commit the pages with the code.
