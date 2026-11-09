# CodeGraph: design (draft for feedback)

**Goal:** turn every line of every `.cs` file into graphs, so you can (1) see what the code does and (2) review
changes, mostly commits written by an AI, faster than with a text diff.

**Split of work:**
- A **generator program** draws every graph from the code. The output is deterministic: same code in, same graph out.
- An **AI** (or you) only adds **descriptions and images** on top. These are stored separately and never change
  the graph.

Status: prototype plus samples. Nothing here is final until you've given feedback.

- Samples: [`sample/graph/index.md`](sample/graph/index.md) · [`Enemy.cs`](sample/graph/Enemy.cs.md) ·
  [`Player.cs`](sample/graph/Player.cs.md) · [`Map/Hole.cs`](sample/graph/Map/Hole.cs.md) ·
  [`Map/DungeonMap.cs`](sample/graph/Map/DungeonMap.cs.md)
- Sample diff of the "Holes" commit `b47c315`: [`sample/diff-b47c315-holes.md`](sample/diff-b47c315-holes.md)

---

## 1. What you get

Four views, from overview down to every line. The structure follows Shneiderman's "overview first, zoom and
filter, then details on demand". Each view stays within a size budget: about 20 nodes, at most about 50.

| View | Answers | Form |
|---|---|---|
| **Index** | How big is each folder, and who depends on whom? What does Unity run, and when? | Folder map (6 nodes, edges labelled with counts) · a Unity lifecycle table (class × phase) · a file table |
| **File page: "Who calls what"** | What calls into this file, and what does it call? | A *butterfly* map: callers and Unity on the left, the file's methods in the middle, the classes they use on the right |
| **File page: every method** | What happens, step by step? | One flowchart per method. **Every statement is in exactly one node** (the build checks this: 738 of 738) |
| **Diff report** | What did this commit change, what could it break, and does it match the commit message? | A ranked text list · independent parts · an impact map · a before/after flowchart per changed method |

Why this order: the research says the most-used view is a **call graph centred on one thing**, with its callers and
callees. Developers ask "what can reach this / what does this reach" about 9 times a day (LaToza & Myers).
Whole-program graphs become unreadable "hairballs", which is why Sourcetrail and NDepend dropped them. Flowcharts
come next. They help most when they show decision structure: Scanlan found people understood structured flowcharts
faster than pseudocode. A flowchart that just restates simple code adds little (Shneiderman, 1977). So the file
page leads with the map, and every line is in a flowchart below it.

## 2. Flowchart rules (one per method)

| C# | Drawn as |
|---|---|
| Method start | Rounded pill with the signature. Methods Unity calls get ⚡ and say *when* ("Unity: every frame") |
| Consecutive plain statements | **One box** (a basic block), one statement per line, at most 4 |
| `if (c) oneStatement;` | **One "guarded step" node**: `if c → statement`. It matches your one-line style: one source line, one node |
| `if` with a block or an `else` | Hexagon `cond ?` with yes/no edges |
| `if (c) return;` (guard clause) | The main path continues **straight down**; the exit goes **to the side** (DRAKON: "the main path doesn't bend") |
| `switch` | A **vertical chain** of `case X ?` checks. Each body merges into a small dot, and the dots form a rail. A wide fan-out gets shrunk to unreadable on GitHub |
| `for` / `foreach` / `while` | Purple ↻ box. The repeat edge is the only edge going up, and it's dotted |
| `yield return …` (coroutines) | ⏳ slanted box. The flow goes on after it (a wait, not an exit) |
| `return`, `yield break`, `throw` | A small end pill **where it happens** (no long edges to one shared end) |
| Calls into your own code | Double-edged box. A **Calls:** line under the chart links to each target |
| Your `// comments` | Shown in *italics* in the node. They are your words, not the AI's |
| `&&`, `\|\|`, `?:`, `??` | Kept inside the node (NIST: hides expression-level noise, keeps the shape readable) |

Labels are your code, normalized (`if(x&&y)` → `x && y ?`), set in monospace, and wrapped at about 48 characters.
Very short straight-line members (1–2 steps, no branch) go in a **"Short members"** table, not 3-box charts.

## 3. File map ("who calls what")

- **Left:** `Unity · every frame / physics / start-up / editor` pills, and the other classes that call in. A dotted
  "virtual" edge means the call arrives through a base-class method.
- **Middle:** this file's methods. ⚡ = Unity calls it, ⏳ = coroutine, 🔁 = runs every frame, i.e. it can be reached
  from `Update` / `FixedUpdate` / … (worth knowing for performance, as Rider marks it too).
- **Right:** each other class is **one node listing the members used**, not one node per member. This keeps the
  view near 20 nodes (aggregate, don't drop).
- **Hidden effects are shown.** If writing a property runs code, it counts as a call. `HP -= dmg` runs
  `Player.HP.set`, which updates the UI, and the map shows that.
- A file with several classes gets one map per class (`Player.cs`).

## 4. `AI/` is treated as external

`AI/` (`PathFinding`, `PlayModeOnly`, `UnifiedChat`) is drawn **like a library**:
- one dashed group that lists only the members your code uses;
- no file pages or flowcharts of its insides;
- the diff report shows only its **API changes** (members added or removed, signatures changed), plus a count of
  bodies that changed inside it.

This combines Visual Studio's "Externals" group, Sourcetrail's non-indexed nodes and dependency-cruiser's
`--do-not-follow`. Which folders count as external is set in [`codegraph.json`](codegraph.json): `"external": ["AI/*"]`.

> ❓ **Please confirm.** I read "treat /ai as external" as "the `AI/` folder is like an outside package". If you
> meant something else (for example, "keep the AI's notes in a folder called `ai`"), tell me.

## 5. The AI layer (descriptions and images)

Notes live in `notes/<source path>.yml`, e.g. [`notes/Enemy.cs.yml`](notes/Enemy.cs.yml). The generator only
**reads** them.

```yaml
Enemy.Update:                     # an id from the graph page: a class, a method, or method#node
  kind: summary                   # summary | usage | rationale | warning
  text: >                         # 2–3 lines: what it is for, how to use it. Not a retelling of the code
    It spawns as a trigger and turns solid once nothing overlaps it ...
  mentions: [PathFinding.Direction, PathFinding.Separation]   # checked: each id must exist
  image: {path: images/enemy-update.svg, alt: "..."}           # optional; SVG for sketches, PNG/WebM for gameplay
  by: ai                          # ai | human
  anchor: {hash: d1033f11, commit: fee0cf8}   # the code this note was written for
  review: {status: unreviewed}                # you can mark it reviewed
```

Rules, each from the research:
- **The AI never draws structure.** AI repo wikis (DeepWiki, ChatGPT diagrams) invent layers and components. Here
  the AI writes only text and images, attached to nodes that already exist.
- **Status is computed on every run, never stored.** It is one of:
  - ✅ *matches the code*;
  - ⚠️ *code changed since this note* (the hash differs, as Swimm does);
  - ❓ *orphaned* (the code is gone). Orphans are listed, never hidden: Hypothesis users thought their notes were lost.
- **Every claim is checkable.** Identifiers in `mentions` must resolve, or `check` fails. In one study, 31.6% of
  LLM code summaries contained a made-up or wrong claim (ETF, 2024).
- **Notes are always labelled** 🤖 AI note · unreviewed, until you review them. MDN said that launching its AI
  feature "without a clear indication of its experimental nature, was a mistake".
- **Where notes go:** method level (85% of developers want these) and class level (62%). Statement-level notes are
  rare (28%), so flowchart nodes keep showing code, not AI prose.
- **Images** (the "AI adds image" part):
  - SVG sketches of *behaviour* that code can't show, like [how an enemy walks around a rock](notes/images/enemy-update.svg)
    or [what happens in a hole](notes/images/hole-fall.svg);
  - each image has a white background, so it stays readable in GitHub dark mode, and alt text;
  - under 100 KB; for gameplay clips, use MP4/WebM rather than GIF.
- **The sample shows drift on purpose:** the `Player.Died` note was written for the code before `b47c315`, so its
  page shows ⚠️.

**AI workflow:** after changing code, run `codegraph notes`. It lists missing, drifted and orphaned notes with the
current hashes. Then, for each note:
- re-check it and re-stamp it if it is still accurate;
- edit it minimally;
- or delete it.

Notes are changed in the same commit as the code.

## 6. Diff: compare two commits as graphs

`python CodeGraph~/tool/codegraph.py diff <base> [<head>]` (with no `<head>`, it compares against your uncommitted
work).

**Matching** works the way BinDiff, GumTree and RefactoringMiner do:
1. Files by path; classes by name.
2. Methods by id (`Type.Method`, plus parameter types for overloads).
3. Leftover methods:
   - same class and name → *signature changed*;
   - identical code → *renamed / moved*.
4. Inside a matched method, statements are aligned per block. Leftovers of the same kind that are at least 50%
   similar → *changed*, shown with `was: <old text>`.

**One union graph per view, with changes coloured.** Side-by-side views "almost never outperformed" overlays for
spotting local changes, and animation confused people. Removed steps stay where they were, dashed. Unchanged steps
are grey context.

**Colours:**

| Change | Colour | Other cues (so colour is never the only signal) |
|---|---|---|
| added | blue | `+`, thick border |
| removed | vermilion | `−`, dashed border |
| changed | yellow | `~`, plus `was: old code` |
| not changed, but calls changed code | white | dotted outline |

The colours are Okabe-Ito, safe for red-green colour blindness, which is why GitHub's colourblind themes also swap
red/green for blue/orange.

**The report starts with text**, which also works if a diagram fails to render. Each change is ranked:

| Rank | Means |
|---|---|
| 🟥 | signature changed, or a public method removed (callers may break) |
| 🟧 | new methods or Unity entry points; changed conditions, branches or calls |
| 🟨 | plain statements |
| ⬜ | comments / formatting only |

The text list also flags:
- 🔁 changes to per-frame code;
- ⚠️ **changed defaults of serialized fields**. Unity keeps the old value in scenes and prefabs, so changing
  `pathFinding = true` in code doesn't turn it on in an existing scene;
- **separate parts**: changes that don't call or share state with each other. 39.9% of Claude Code PRs mix several
  purposes, against 12.2% of human PRs, so check each part against the commit message.

**Impact map:** the changed methods and state, plus one step of context, plus unchanged callers that may now
behave differently.

**Why the generator draws the diff itself:** GitHub doesn't diff diagrams: "Show diff" just renders the new
version, and nothing documents rich diff rendering Mermaid.

## 7. Files, Unity, GitHub

```
CodeGraph~/                 Unity ignores folders ending in "~": nothing is compiled, no .meta files
  DESIGN.md  README.md
  codegraph.json            external / ignore settings
  tool/                     the generator (Python 3.10+, tree-sitter C# parser): pip install -r requirements.txt
  notes/                    the AI / human layer (yml + images): the only part a person or the AI writes
  graph/                    generated pages (after feedback; the samples are in sample/)
```

- Output is **Mermaid inside Markdown**, so GitHub draws it on the web and in PRs with nothing to install. GitHub
  ran Mermaid 11.16 in Aug 2026.
- Only classic syntax is used.
- Every style sets its text colour, so dark mode works.
- Each diagram stays far below Mermaid's default limits (50 000 characters, 500 edges), which a diagram can't
  raise on GitHub. The largest sample diagram is about 4 000 characters.
- **Diff-friendly output:** ids are content-based, not 1, 2, 3, so inserting a line doesn't renumber everything.
  Nothing in the pages depends on the date.

## 8. Known limits

- **The type resolver is heuristic, not a compiler.** It follows declared types, `var`, `out`/`is` variables,
  `foreach` and `GetComponent<T>`/`Instantiate`. Calls into Unity or packages simply aren't drawn.
- **Scene and prefab wiring is invisible to code.** This covers buttons, UnityEvents, and `SendMessage` by name.
  Public methods with no caller in code are marked *"none in code (Inspector / UnityEvent?)"*.
- **Mermaid's layout (dagre) can't be pinned.** Usually it's fine; very branchy methods can get wide. Switches are
  already handled.
- **`Data`** (used by `GameManager` and `ChannelBtn`) isn't defined in this repo, so it shows as unresolved.

## 9. Questions for you

1. **`AI/` as external:** is my reading right (§4)?
2. **Commit the pages?** I recommend committing `graph/`, so you can browse it on GitHub. The AI regenerates it with
   every code change, and it would be collapsed in PR diffs (`linguist-generated`). The alternative is generating
   it only when needed.
3. **Diff delivery:** a diff report in each PR description (GitHub renders it); a file per commit; on request only;
   or a GitHub Action that comments on PRs.
4. **Flowchart text:** keep the code and your comments in the nodes, with AI notes per method (recommended,
   research-backed)? Or do you want plain-English AI labels on the nodes too?
5. **Short members** as one table row instead of tiny charts: OK?
6. **Colours:** blue/vermilion/yellow (colourblind-safe) or GitHub's green/red?
7. **Later, optional:** an interactive HTML viewer (search, click a call to jump, show/hide notes).

## Sources (main ones)

Code maps:
- Sourcetrail docs: https://github.com/CoatiSoftware/Sourcetrail/blob/master/DOCUMENTATION.md
- C4 model FAQ: https://c4model.com/faq
- Shneiderman, the visual information-seeking mantra: https://webspace.science.uu.nl/~telea001/uploads/VACourse/Shneiderman96.pdf
- LaToza & Myers, ICSE 2010: https://memphis-cs.github.io/comp-7085-8085-2012-fall/papers/LaToza2010ICSE.pdf
- NDepend dependency graph: https://www.ndepend.com/docs/visual-studio-dependency-graph
- Graph readability study: https://arxiv.org/abs/2008.07944
- Rider, Unity performance-critical code: https://www.jetbrains.com/help/rider/Unity.PerformanceCriticalCodeInvocation.html

Flowcharts:
- DRAKON rules: https://drakon-editor.sourceforge.net/summary.html
- Purchase, which aesthetic matters most: https://research.monash.edu/en/publications/which-aesthetic-has-the-greatest-effect-on-human-understanding/
- CFGConf: https://arxiv.org/pdf/2108.03047v2
- Royer et al., better CFG layouts: https://www.ndss-symposium.org/wp-content/uploads/bar2025-final11.pdf
- NIST SP 500-235: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication500-235.pdf
- Mermaid flowchart syntax: https://mermaid.js.org/syntax/flowchart.html
- Mermaid config: https://mermaid.js.org/config/schema-docs/config.html

Diffs:
- L'Yi et al., comparison layouts: https://arxiv.org/pdf/2009.00192v1.pdf
- Sambasivan et al., graph diff views: https://www.pdl.cmu.edu/PDL-FTP/ProblemDiagnosis/pathception_infovis_2013.pdf
- GumTree: https://www.labri.fr/perso/xblanc/data/papers/ASE14.pdf
- BinDiff manual: https://www.zynamics.com/bindiff/manual/index.html
- Watanabe et al., agent PRs: https://arxiv.org/pdf/2509.14745
- Okabe & Ito palette: https://jfly.uni-koeln.de/color/
- GitHub colourblind themes: https://github.blog/changelog/2021-09-29-colorblind-themes-beta/
- GitHub, rich diff doesn't render Mermaid: https://github.com/orgs/community/discussions/121855

AI notes:
- Swimm patent application: https://data.epo.org/publication-server/rest/v1.2/patents/EP4546120NWA1/document.html
- Hypothesis, orphaned annotations: https://web.hypothes.is/blog/showing-orphaned-annotations
- ETF, hallucinations in code summaries: https://arxiv.org/html/2410.14748v4
- Hu et al., ICSE 2022: https://xing-hu.github.io/assets/papers/ICSE22.pdf
- MDN AI Explain postmortem: https://developer.mozilla.org/en-US/blog/ai-explain-postmortem
- GitLab images style guide: https://docs.gitlab.com/development/documentation/styleguide/images_and_diagrams/
