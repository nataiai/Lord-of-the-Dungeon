# Flow~

Data for **Dungeon Flow**, a click-through map of these scripts (Unity order, each script's life
cycle, call trees, data wires, the real lines of code). Unity skips this folder (the name ends in `~`),
so nothing here gets a `.meta` file or touches the game.

| File | What |
|---|---|
| `flow.json` | Claude's map: titles, descriptions and line ranges for each box. No code text: the page reads the lines from the `.cs` files. Its `_about` field describes the format. |
| `vscode/` | A small VS Code extension: a box in the page opens its lines in VS Code with a box drawn around them. Package with `npx @vscode/vsce package`, install with `code --install-extension`. Not tested in VS Code yet. |

The page's code (the C# reader that builds the generated map, the page, the build and the checks),
and the full record of every change, its reason and the mistakes to avoid, are in the
`everything-on-trial` repo, branch `dungeon-flow`, folder `dungeon-flow/`. Read its `JOURNEY.md`
("Before you deliver") before changing anything here, and run its `build.js` after editing
`flow.json`: it checks every range and every call against the code.
