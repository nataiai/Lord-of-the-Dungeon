# CodeGraph

Graphs of every `.cs` file. A generator program draws them; an AI (or you) adds notes and images on top.
Unity ignores this folder, because its name ends in `~`.

Read [DESIGN.md](DESIGN.md) for what each view shows and why. Samples are in [sample/](sample/).

## Run

You need Python 3.10 or newer. Install the requirements once:

```
pip install -r "CodeGraph~/tool/requirements.txt"
```

Then run from the repo root:

```
python "CodeGraph~/tool/codegraph.py" build                  # write the pages to CodeGraph~/graph
python "CodeGraph~/tool/codegraph.py" diff HEAD~1 HEAD       # graph diff of the last commit
python "CodeGraph~/tool/codegraph.py" diff                   # graph diff of your uncommitted work
python "CodeGraph~/tool/codegraph.py" notes                  # notes to write or re-check (the AI's to-do list)
python "CodeGraph~/tool/codegraph.py" check                  # fails on orphaned notes or broken mentions
```

## Folders

- `tool/`: the generator.
- `notes/`: AI and human notes (`.yml`) and their images. This is the only part anyone writes by hand.
- `graph/`: generated pages. Don't edit them; run `build` again instead.
- `sample/`: the design samples.
