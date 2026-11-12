# Write code like the owner, not like an AI

Unity C# scripts. The owner's style is in the first commit (`c470dd7`).
The owner reads code outside `AI/`; inside `AI/` they barely look.

## Code
- Readable is what counts: any code is fine if it reads at a glance.
- Short lines; a field's name is visible without scrolling right.
- One-liners are fine: `if(HP<=0)Died();` `if(...){do();return;}`
- Small function: plain if/else. Long function: shorten it with `?:`.
- Few, short comments (about 1 line in 80): `//test`, `// for ui`, `// 0 = no limit`.
  No comment on every line, no header essays.
- Inspector help: `[Tooltip("...")]` on its own line above the field.
- New singletons: `X.Instance`.

## Changes
- Many diffs annoy the owner. Change as few lines as possible; never reformat working code.
- Comments: never reword one. Delete it or leave it.
- New logic in `AI/`, a one-line hook in the owner's files.
- "Least code" = less logic, not cramming lines.
- Keep default usings (`System.Collections`, `System.Collections.Generic`, `UnityEngine`).
  Don't add usings; write `System.X` in place.
- Keep empty `Start`/`Update`, commented-out code, unused fields/functions ("i'll use those later").
- Keep names and typos (`recieveDamage`, `knocback`, `d_` = dirty). Rename only if misleading,
  with `[FormerlySerializedAs]`.
- Push to `main` only when asked. Few commits, short messages, tl;dr reports.
