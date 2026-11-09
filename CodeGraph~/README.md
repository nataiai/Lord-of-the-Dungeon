# CodeGraph

Every line of C# in this folder drawn as graphs, so you can see what the code does and review changes as pictures instead of reading diffs line by line.

**Start here:** [graphs/README.md](graphs/README.md): the map of all classes, then one page per `.cs` file (a graph per class, a flowchart per method).

## Who writes what

| Folder | Written by | What is in it |
|---|---|---|
| `graphs/` | `codegraph.py` only. Never edit. | One page per `.cs` file, and the overview |
| `ai/` | The AI (or you). The generator only reads it. | Descriptions and pictures shown between the graphs |
| `out/` | `codegraph.py diff`. Not committed. | Graph diffs: `diff.md` and `diff.html` |
| `AI/` (next to the scripts) | Code written by the AI | Treated as external: drawn as outside boxes, no pages of its own |

The program draws, the AI describes: the graphs come straight from the code, so they are never wrong or out of date once regenerated, and the same code always gives byte-identical pages. External folders are set in [codegraph.json](codegraph.json); remove `AI/` there to graph it like the rest.

## Commands

Python 3.8 or newer, nothing to install. Run them from the scripts folder (the repository root):

| Command | What it does |
|---|---|
| `python "CodeGraph~/codegraph.py"` | Redraws `graphs/` from the code and the notes |
| `python "CodeGraph~/codegraph.py" diff` | Graph diff of what you changed since the last commit |
| `python "CodeGraph~/codegraph.py" diff HEAD~1 HEAD` | Graph diff of the last commit. Any two revisions or branches work |
| `python "CodeGraph~/codegraph.py" todo` | Notes that are missing, out of date, or no longer match any code |
| `python "CodeGraph~/codegraph.py" check` | Exits with 1 if `graphs/` is out of date (for CI or a pre-commit hook) |

## Viewing

- **GitHub** draws Mermaid: open `CodeGraph~/graphs` there. Graphs can be zoomed and panned.
- **Locally**: VS Code with the "Markdown Preview Mermaid Support" extension, or Rider with Mermaid turned on in the Markdown settings.
- **Diffs**: open `out/diff.html` in a browser (it loads Mermaid from a CDN), or paste `out/diff.md` into a pull request or issue, where GitHub draws it. Green boxes are added, red removed, yellow changed; a changed line also shows what it was.

## Notes in ai/

One file per source file, at the same path plus `.md`: `ai/Map/Chest.cs.md` describes `Map/Chest.cs`. `ai/README.md` is the project overview at the top of `graphs/README.md`.

```markdown
# Map/Chest.cs <!-- d547ab -->
What the file is for. This first line also appears in the file table.

## Chest <!-- b9caf8 -->
One short line: the caption inside this class's box on the overview graph.
Then anything: more text, lists, links, images ![open chest](images/chest-open.png), Mermaid blocks.

## Chest.Died <!-- 8062f8 -->
What this method does.
```

- Headings are `# path` for the file, `## Class` for a class, `## Class.Method` for a method (overloads add their parameter types, `## Spawner.Spawn(Enemy,Vector2,float)`; property code is `## Player.HP.set`). `todo` prints the exact heading lines to use.
- `<!-- d547ab -->` is a fingerprint of the code the note describes. When that code changes, its page marks the note as possibly out of date and `todo` lists it with the new fingerprint: check the text, then update the fingerprint.
- Put images in `ai/images/` and link to them relative to the note; the generator fixes the paths for the page that shows them.
- Inside a section, use bold text or `###` for sub-headings: a `##` line starts a new section.

## Limits

- It reads C# with its own small parser rather than the compiler. The pages list any code line that did not land in a graph, and the overview counts them (none today).
- Calls are found through the declared types of fields, parameters, locals and `GetComponent<T>()`. Calls made through UnityEvents set in the Inspector, `SendMessage`, reflection, or lambda parameters without a type are not seen.
- Both branches of an `#if` block are drawn.
- The folder name ends with `~`, so Unity ignores it: no `.meta` files, nothing in builds.
