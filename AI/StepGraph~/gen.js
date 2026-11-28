#!/usr/bin/env node
// Step graph of a commit. Run inside the repo:  node gen.js [old=HEAD~1] [new=HEAD] > graph.html   (new can be WORK = files on disk)
// It reads every .cs at both commits, follows the calls out of each Unity step function, and compares the two graphs.
const cp = require("child_process"), fs = require("fs"), path = require("path");
const git = (...a) => cp.execFileSync("git", a, {encoding:"utf8", maxBuffer:1 << 28});
const [OLD = "HEAD~1", NEW = "HEAD"] = process.argv.slice(2);

const STEPS = [
  ["Awake", /^Awake$/, "Once, before any Start."],
  ["Start", /^Start$/, "Once, before the object's first Update."],
  ["Update", /^Update$/, "Every frame."],
  ["FixedUpdate", /^FixedUpdate$/, "Every physics step."],
  ["LateUpdate", /^LateUpdate$/, "Every frame, after all Updates."],
  ["Trigger", /^On(Trigger|Collision)/, "OnTrigger / OnCollision: when colliders touch."],
  ["Enable", /^On(Enable|Disable|Destroy)$/, "When an object is turned on, off or destroyed."],
];

function readTree(rev){ // path -> text
  const out = {};
  if(rev === "WORK"){ for(const f of git("ls-files", "-z", "*.cs").split("\0").filter(Boolean)) if(fs.existsSync(f)) out[f] = fs.readFileSync(f, "utf8"); return out; }
  for(const f of git("ls-tree", "-r", "-z", "--name-only", rev).split("\0")) if(f.endsWith(".cs")) out[f] = git("show", `${rev}:${f}`);
  return out;
}

// comments and string contents become spaces (calls inside them are not code). Same length, so a slice of it is a slice of the file
function strip(s){
  let out = "", i = 0;
  const blank = t => t.replace(/[^\n]/g, " ");
  while(i < s.length){
    const c = s[i], d = s[i + 1];
    if(c === "/" && d === "/"){ let e = s.indexOf("\n", i); if(e < 0) e = s.length; out += blank(s.slice(i, e)); i = e; }
    else if(c === "/" && d === "*"){ const e = s.indexOf("*/", i + 2); const end = e < 0 ? s.length : e + 2; out += blank(s.slice(i, end)); i = end; }
    else if(c === '"' || ((c === "@" || c === "$") && (d === '"' || s[i + 2] === '"'))){
      const verbatim = s.slice(i, i + 3).replace(/".*/, "").includes("@"), q = s.indexOf('"', i);
      let j = q + 1;
      while(j < s.length){
        if(verbatim){ if(s[j] === '"'){ if(s[j + 1] === '"') j += 2; else break; } else j++; }
        else if(s[j] === "\\") j += 2; else if(s[j] === '"') break; else j++;
      }
      out += s.slice(i, q + 1) + blank(s.slice(q + 1, j)) + '"'; i = j + 1;
    }
    else if(c === "'"){ let j = i + 1; if(s[j] === "\\") j += 2; while(j < s.length && s[j] !== "'") j++; out += "'" + " ".repeat(j - i - 1) + "'"; i = j + 1; }
    else{ out += c; i++; }
  }
  return out;
}
let curFile = "", curRanges = [], curStarts = []; // curStarts: where each line of the file begins // the file members() is reading, and where its functions are
function dedent(t){
  const lines = t.split("\n"), ind = Math.min(...lines.slice(1).filter(l => l.trim()).map(l => l.match(/^\s*/)[0].length));
  return isFinite(ind) ? [lines[0], ...lines.slice(1).map(l => l.slice(ind))].join("\n") : t;
}
function closer(s, i){ // index of the bracket that closes the one at i
  const open = s[i], close = {"{":"}", "(":")", "[":"]", "<":">"}[open]; let n = 0;
  for(; i < s.length; i++){ if(s[i] === open) n++; else if(s[i] === close && !--n) return i; }
  return s.length - 1;
}
function topLevel(s, sep){ // split on sep outside brackets
  const out = []; let n = 0, last = 0;
  for(let i = 0; i < s.length; i++){
    if("([{<".includes(s[i])) n++; else if(")]}>".includes(s[i])) n--;
    else if(s[i] === sep && n <= 0){ out.push(s.slice(last, i)); last = i + 1; }
  }
  out.push(s.slice(last)); return out;
}
const TYPE = /([A-Za-z_][\w.]*(?:<[^;(){}=]*>)?(?:\[[\s,]*\])*\??)\s+(\w+)\s*$/;
const plain = t => (t || "").replace(/<.*>/, "").replace(/\[.*\]|\?/g, "").split(".").pop();
function elem(t){ // List<X>, X[] -> X
  if(!t) return t;
  if(/\[[\s,]*\]$/.test(t)) return t.replace(/\[[\s,]*\]$/, "");
  const m = t.match(/^[\w.]+<(.*)>$/); return m ? topLevel(m[1], ",").pop().trim() : t;
}

function members(body, raw, cls, base){
  let n = 0, start = 0;
  for(let i = 0; i < body.length; i++){
    const c = body[i];
    if("({[".includes(c)) n++;
    else if(")]".includes(c)) n--;
    else if(c === "}"){ if(--n === 0 && !/^\s*[;=,.)]/.test(body.slice(i + 1))){ member(body.slice(start, i + 1), raw.slice(start, i + 1), cls, base + start); start = i + 1; } }
    else if(c === ";" && n === 0){ member(body.slice(start, i + 1), raw.slice(start, i + 1), cls, base + start); start = i + 1; }
  }
}
function member(seg, raw, cls, at){
  const lead = seg.length - seg.trimStart().length; seg = seg.trim(); raw = raw.slice(lead, lead + seg.length); at += lead; let m;
  while((m = seg.match(/^\[[^\]]*\]\s*/))){ seg = seg.slice(m[0].length); raw = raw.slice(m[0].length); at += m[0].length; }
  let line = 1; for(let lo = 0, hi = curStarts.length - 1; lo <= hi;){ const mid = (lo + hi) >> 1; if(curStarts[mid] <= at){ line = mid + 1; lo = mid + 1; } else hi = mid - 1; }
  const code = {file:curFile, text:dedent(raw), line}; // line: the file line it starts on
  let n = 0, cut = seg.length, kind = ";";
  for(let i = 0; i < seg.length; i++){
    const c = seg[i];
    if("([<".includes(c)) n++; else if(")]>".includes(c) && seg[i - 1] !== "=") n--;
    else if(n <= 0 && (c === "{" || c === ";" || (c === "=" && !"=!<>".includes(seg[i - 1]) && seg[i + 1] !== "="))){ cut = i; kind = c === "=" && seg[i + 1] === ">" ? "=>" : c; break; }
  }
  const head = seg.slice(0, cut).trim(), rest = seg.slice(cut);
  if(/\b(class|struct|interface|enum|delegate|namespace|using)\b/.test(head)) return;
  const call = head.match(/^(.*?)(\w+)\s*(?:<[^>]*>)?\s*\((.*)\)\s*(?:where\b.*)?$/s);
  if(call && (kind === "{" || kind === "=>")){
    const ret = (call[1].trim().match(/([\w.<>\[\],?]+)$/) || [])[1];
    if(!ret || call[2] === cls.name || /^(public|private|protected|internal|static)$/.test(ret)) return; // constructor
    const meth = cls.methods[call[2]] ||= {ret, bodies:[], params:{}, code:[]};
    for(const p of topLevel(call[3], ",")){ const t = p.split("=")[0].match(TYPE); if(t) meth.params[t[2]] = t[1]; }
    meth.bodies.push(rest); meth.code.push(code); curRanges.push([at, at + seg.length, raw.slice(0, cut).trim()]); return;
  }
  if(kind === "{" || kind === "=>"){ const t = head.match(TYPE); if(t){ cls.props[t[2]] = {type:t[1], body:rest, code:[code]}; curRanges.push([at, at + seg.length, raw.slice(0, cut).trim()]); } return; }
  const parts = topLevel(seg.replace(/;$/, ""), ","), first = parts[0].split(/=(?!=)/)[0].match(TYPE);
  if(!first) return;
  cls.fields[first[2]] = first[1];
  for(const p of parts.slice(1)){ const nm = p.split("=")[0].trim(); if(/^\w+$/.test(nm)) cls.fields[nm] = first[1]; }
}

function analyse(files){
  const classes = {};
  const ranges = {};
  for(const [file, raw] of Object.entries(files)){
    curFile = file; curRanges = ranges[file] = []; curStarts = [0]; for(let i = 0; i < raw.length; i++) if(raw[i] === "\n") curStarts.push(i + 1);
    const s = strip(raw), re = /\b(?:class|struct)\s+(\w+)(?:\s*<[^>{]*>)?\s*(?::\s*([^{]+?))?\s*(?:where[^{]*)?\{/g; let m;
    while((m = re.exec(s))){
      const open = m.index + m[0].length - 1, end = closer(s, open), body = s.slice(open + 1, end);
      const cls = classes[m[1]] ||= {name:m[1], file, folder:path.dirname(file).replace(/^\.$/, ""), bases:[], fields:{}, props:{}, methods:{}};
      if(m[2]) cls.bases.push(...topLevel(m[2], ",").map(b => plain(b.trim())));
      members(body, raw.slice(open + 1, end), cls, open + 1);
    }
  }
  const base = c => classes[c]?.bases.find(b => classes[b]);
  const up = c => { const out = []; for(; c && classes[c]; c = base(c)) out.push(c); return out; };
  const kids = {}; for(const c in classes) for(const a of up(c).slice(1)) (kids[a] ||= []).push(c);
  const memberType = (c, name) => { for(const k of up(c)){ const t = classes[k].fields[name] ?? classes[k].props[name]?.type; if(t) return t; } };
  const owner = (c, name, what) => up(c).find(k => classes[k][what][name]);
  const GETTER = /^(GetComponents?(InChildren|InParent)?|FindFirstObjectByType|FindObjectOfType|FindAnyObjectByType|FindObjectsByType|AddComponent|TryGetComponent)$/;

  // "a.b(x).c" read backwards from position i (just before the last name): the chain of things in front of it
  function chainBefore(s, i){
    const out = [];
    for(;;){
      let j = i; while(j > 0 && /\s/.test(s[j - 1])) j--;
      if(s[j - 1] !== ".") return out;
      j--; if(s[j - 1] === "?") j--;
      while(j > 0 && /\s/.test(s[j - 1])) j--;
      const el = {call:false, index:false};
      if(s[j - 1] === "]"){ let n = 0; do{ j--; if(s[j] === "]") n++; else if(s[j] === "[") n--; }while(n && j > 0); el.index = true; }
      if(s[j - 1] === ")"){ let n = 0; do{ j--; if(s[j] === ")") n++; else if(s[j] === "(") n--; }while(n && j > 0); el.call = true; while(j > 0 && /\s/.test(s[j - 1])) j--; }
      if(s[j - 1] === ">"){ let n = 0, e = j; do{ j--; if(s[j] === ">") n++; else if(s[j] === "<") n--; }while(n && j > 0); el.generic = s.slice(j + 1, e - 1).trim(); }
      let e = j; while(j > 0 && /\w/.test(s[j - 1])) j--;
      if(e === j) return null; // (expr).x : unknown
      el.name = s.slice(j, e); out.unshift(el); i = j;
    }
  }
  function typeOf(chain, cls, locals){ // undefined = can't tell
    let t;
    for(const [n, el] of chain.entries()){
      if(n === 0){
        if(el.call) t = GETTER.test(el.name) && el.generic ? el.generic : classes[owner(cls, el.name, "methods")]?.methods[el.name].ret;
        else t = el.name === "this" ? cls : el.name === "base" ? base(cls) : locals[el.name] ?? memberType(cls, el.name) ?? (classes[el.name] ? el.name : undefined);
      }else{
        const T = plain(t);
        if(el.call) t = GETTER.test(el.name) && el.generic ? el.generic : classes[owner(T, el.name, "methods")]?.methods[el.name].ret;
        else t = classes[T] ? memberType(T, el.name) ?? (el.name === "Instance" || el.name === "instance" || el.name === "I" ? T : undefined) : undefined;
      }
      if(t === undefined) return undefined;
      if(el.call && /^(GetComponents|FindObjectsByType)/.test(el.name)) t += "[]";
      if(el.index) t = elem(t);
    }
    return t;
  }

  const nodes = {}, edges = {}, uniq = {}, interesting = new Set();
  for(const c in classes) for(const m in classes[c].methods) (uniq[m] ||= []).push(c);
  for(const c in classes){
    for(const [m, d] of Object.entries(classes[c].methods)) nodes[`${c}.${m}`] = {cls:c, name:m, text:d.bodies.join("\n"), params:d.params, code:d.code};
    for(const [p, d] of Object.entries(classes[c].props)) if(/\w\s*\(/.test(d.body)){ nodes[`${c}.${p}`] = {cls:c, name:p, text:d.body, params:{value:d.type}, code:d.code}; interesting.add(p); }
  }
  const targets = (T, name, what) => { // the definition seen from T, and the overrides below T
    const o = owner(T, name, what), out = o ? [o] : [];
    for(const k of kids[T] || []) if(classes[k][what][name]) out.push(k);
    return out.map(k => `${k}.${name}`).filter(k => nodes[k]);
  };
  const SKIP = /^(if|for|foreach|while|switch|return|new|nameof|typeof|catch|using|lock|sizeof|default|await|yield|when|is|as|in|out|ref|else|do|base|this)$/;
  for(const [key, node] of Object.entries(nodes)){
    const s = node.text, cls = node.cls, locals = {...node.params}, out = edges[key] = new Set(); let m;
    const decl = /(?:\b(?:out|foreach\s*\()\s*)?\b([A-Z_][\w.]*(?:<[^;(){}=]*>)?(?:\[\])?)\s+([a-z_]\w*)\s*(?==[^=]|;|,|\)|\bin\b)/g;
    while((m = decl.exec(s))) if(!SKIP.test(m[1])) locals[m[2]] ??= m[1];
    const vars = /\bvar\s+(\w+)\s*(=|in\b)\s*([^;{]*)/g;
    while((m = vars.exec(s))){ // var x = a.b.c;  var x in list
      const e = m[3].trim().replace(/\)\s*$/, ""), nw = e.match(/^new\s+([\w.]+)/); let t;
      if(nw) t = nw[1];
      else{ const w = e.match(/^[\w.?\s]*\w/); if(w && w[0].length >= e.length - 2 || w && /^[\s)]*$/.test(e.slice(w[0].length).replace(/\([^()]*\)/g, ""))){
        const end = w[0].length, st = w[0].search(/\w+$/), ch = chainBefore(e, st);
        if(ch) t = typeOf([...ch, {name:w[0].slice(st), call:e[end] === "(", index:false}], cls, locals); } }
      if(t) locals[m[1]] ??= m[2] === "=" ? t : elem(t);
    }
    const calls = /\b(\w+)\s*(?:<[^<>;(){}]*>)?\s*\(/g;
    while((m = calls.exec(s))){
      const name = m[1]; if(SKIP.test(name) || /\bnew\s*$/.test(s.slice(0, m.index))) continue;
      const ch = chainBefore(s, m.index);
      if(ch && !ch.length){ for(const t of targets(cls, name, "methods")) out.add(t); continue; }
      if(ch && ch.length === 1 && ch[0].name === "base"){ const o = owner(base(cls), name, "methods"); if(o) out.add(`${o}.${name}`); continue; } // base.X(): no overrides
      const T = ch ? typeOf(ch, cls, locals) : undefined;
      if(T !== undefined){ if(classes[plain(T)]) for(const t of targets(plain(T), name, "methods")) out.add(t); }
      else if(uniq[name]?.length === 1 && name.length > 5) out.add(`${uniq[name][0]}.${name}`); // type unknown: only a name nobody else has
    }
    for(const p of interesting){ // properties that do something when read or set (HP -> UI)
      const re = new RegExp(`\\b${p}\\b(?!\\s*\\()`, "g");
      while((m = re.exec(s))){
        const ch = chainBefore(s, m.index); if(!ch) continue;
        const T = ch.length ? typeOf(ch, cls, locals) : locals[p] ? undefined : cls;
        if(T !== undefined && classes[plain(T)]) for(const t of targets(plain(T), p, "props")) if(t !== key) out.add(t);
      }
    }
    out.delete(key);
  }
  // a property is only worth a box when it calls something of ours (HP -> UI.UpdateHP), not a plain getter or singleton
  const dull = new Set(Object.keys(nodes).filter(k => classes[nodes[k].cls].props[nodes[k].name] && !classes[nodes[k].cls].methods[nodes[k].name] && !edges[k].size));
  for(const k of dull){ delete nodes[k]; delete edges[k]; }
  for(const k in edges) for(const t of dull) edges[k].delete(t);
  return {classes, nodes, edges, ranges, files};
}

function reach(g, root){ // every call that can follow from root
  const seen = new Set([root]), list = [], q = [root];
  while(q.length){ const a = q.shift(); for(const b of g.edges[a] || []){ list.push(`${a} -> ${b}`); if(!seen.has(b)){ seen.add(b); q.push(b); } } }
  return list;
}
function lineDiff(a, b){ // lines only in b, lines only in a
  const count = t => { const m = {}; for(const l of t.split("\n").map(l => l.trim()).filter(Boolean)) m[l] = (m[l] || 0) + 1; return m; };
  const x = count(a), y = count(b); let add = 0, del = 0;
  for(const l in y) add += Math.max(0, y[l] - (x[l] || 0));
  for(const l in x) del += Math.max(0, x[l] - (y[l] || 0));
  return `+${add} -${del}`;
}

const old = analyse(readTree(OLD)), now = analyse(readTree(NEW));

// steps inside a function: the titles in AI/Flow~/flow.json (Dungeon Flow's notes), found by their first line's text
let flowNotes = [];
for(const get of [() => fs.readFileSync("AI/Flow~/flow.json", "utf8"), () => git("show", "origin/dungeon-flow:AI/Flow~/flow.json")]){
  try{ flowNotes = JSON.parse(get()).notes || []; break; }catch(e){}
}
const fileLines = {}, linesOf = f => fileLines[f] ||= (now.files[f] || "").split("\n");
const placed = flowNotes.map(n => { // where the note's first line is now
  const L = linesOf(n.f), want = n.line.trim(); let best = -1;
  for(let i = 0; i < L.length; i++) if(L[i].trim() === want && (best < 0 || Math.abs(i + 1 - n.a) < Math.abs(best + 1 - n.a))) best = i;
  return best < 0 ? null : {f:n.f, t:n.t, a:best + 1, b:best + 1 + (n.b - n.a)};
}).filter(Boolean);
const stepCache = {};
function stepsOf(k){ // [{t, a, b}] in line order: the outermost notes inside the function, not the one that is the whole function
  if(k in stepCache) return stepCache[k];
  const c = now.nodes[k]?.code[0]; if(!c) return stepCache[k] = [];
  const a0 = c.line, b0 = c.line + c.text.split("\n").length - 1;
  const inside = placed.filter(n => n.f === c.file && n.a >= a0 && n.b <= b0 && !(n.a === a0 && n.b === b0) && n.a > a0 - 1);
  const top = inside.filter(n => !inside.some(o => o !== n && o.a <= n.a && o.b >= n.b && (o.a < n.a || o.b > n.b)))
    .sort((x, y) => x.a - y.a).filter((n, i, l) => i === 0 || n.a > l[i - 1].b); // no overlaps
  if(top.length < 2) return stepCache[k] = [];
  // lines no note covers that call one of our functions or were added: a step showing the code itself
  const fns = new Set(Object.values(now.nodes).map(n => n.name)), added = new Set(addedLines(k)), lines = c.text.split("\n");
  for(let i = 1; i < lines.length - 1; i++){
    const L = c.line + i, code = strip(lines[i]).trim(); if(!code || code === "}" || top.some(n => L >= n.a && L <= n.b)) continue;
    const calls = [...code.matchAll(/\b(\w+)\s*\(/g)].some(m => fns.has(m[1]));
    if(calls || added.has(L)) top.push({t:"`" + (code.length > 34 ? code.slice(0, 33) + "…" : code), a:L, b:L});
  }
  return stepCache[k] = top.sort((x, y) => x.a - y.a);
}
function addedLines(k){ // file lines added or right after a removed line, in function k
  const c = now.nodes[k]?.code[0]; if(!c || !old.nodes[k]) return c ? c.text.split("\n").map((_, i) => c.line + i) : [];
  const out = []; let i = 0;
  for(const l of diff(codeOf(old, k), codeOf(now, k))){ if(l[0] === "…") i += +l.slice(1); else if(l[0] === "-") out.push(c.line + i); else { if(l[0] === "+") out.push(c.line + i); i++; } }
  return out;
}
const callLines = (k, target) => { // file lines in function k that call target
  const c = now.nodes[k]?.code[0], name = target.split(".")[1], re = new RegExp(`\\b${name}\\b`); if(!c) return [];
  return c.text.split("\n").map((l, i) => re.test(strip(l)) && i > 0 ? c.line + i : 0).filter(Boolean);
};
function stepMarks(k){ // which steps changed: lines added or removed inside them
  const st = stepsOf(k), m = markOf(k); if(!st.length || !m) return st.map(() => "");
  if(m === "+") return st.map(() => "+");
  const hit = addedLines(k);
  return st.map(s => hit.some(h => h >= s.a && h <= s.b) ? (s.a === s.b && s.t[0] === "`" && !old.nodes[k]?.code[0].text.includes(s.t.slice(1).replace(/…$/, "")) ? "+" : "~") : "");
}
const codeOf = (g, k) => g.nodes[k] ? g.nodes[k].code.map(c => c.text).join("\n\n") : "";
const markOf = k => !old.nodes[k] ? "+" : !now.nodes[k] ? "-" : codeOf(old, k).replace(/\s+/g, "") !== codeOf(now, k).replace(/\s+/g, "") ? "~" : "";
// lines: " x" same, "-x" only old, "+x" only new, "…n" n same lines left out
function diff(a, b){
  const A = a ? a.split("\n") : [], B = b ? b.split("\n") : [], n = A.length, m = B.length, eq = (x, y) => x.trim() === y.trim();
  const L = Array.from({length:n + 1}, () => new Int32Array(m + 1));
  for(let i = n - 1; i >= 0; i--) for(let j = m - 1; j >= 0; j--) L[i][j] = eq(A[i], B[j]) ? L[i + 1][j + 1] + 1 : Math.max(L[i + 1][j], L[i][j + 1]);
  const out = []; let i = 0, j = 0;
  while(i < n || j < m){
    if(i < n && j < m && eq(A[i], B[j])){ out.push(" " + B[j]); i++; j++; }
    else if(i < n && (j >= m || L[i + 1][j] >= L[i][j + 1])) out.push("-" + A[i++]); // old line first
    else out.push("+" + B[j++]);
  }
  const near = out.map((l, k) => k === 0 || out.slice(Math.max(0, k - 2), k + 3).some(x => x[0] !== " ")); // 2 lines around a change, and the first
  const res = []; let skip = 0;
  out.forEach((l, k) => { if(near[k]){ if(skip) res.push("…" + skip); skip = 0; res.push(l); } else skip++; });
  if(skip) res.push("…" + skip);
  return res;
}
const code = {}; // what the page shows when a function is tapped
const addCode = k => { if(code[k]) return;
  const m = markOf(k), g = now.nodes[k] ? now : old;
  code[k] = {f:g.nodes[k].code[0]?.file || "", m};
  if(m) code[k].d = diff(codeOf(old, k), codeOf(now, k)); else code[k].t = codeOf(now, k);
};
const STEP_FN = /^(Awake|Start|Update|FixedUpdate|LateUpdate|On(Trigger|Collision|Enable|Disable|Destroy).*)$/;
const shown = k => true; // every function with steps; the page shows them when zoomed in
function fromStep(line){ // "A -> B" -> one line per step of A that calls B ("A#2 -> B")
  const m = line.match(/^([+-] )?(\S+) -> (\S+)$/); if(!m) return [line];
  const [, mk = "", a, b] = m, st = shown(a) ? stepsOf(a) : []; if(!st.length) return [line];
  const at = callLines(a, b), idx = [...new Set(at.map(l => st.findIndex(s => l >= s.a && l <= s.b)).filter(i => i >= 0))];
  return idx.length ? idx.map(i => `${mk}${a}#${i + 1} -> ${b}`) : [line];
}
function source(pattern, used, parts, degree){ // the bracket text the page draws
  const byClass = {}; for(const k of used) (byClass[k.split(".")[0]] ||= []).push(k);
  const info = c => now.classes[c] || old.classes[c];
  const leaf = k => { const m = markOf(k), st = shown(k) ? stepsOf(k) : [], sm = stepMarks(k);
    return m + k.split(".")[1] + (m === "~" ? ": " + lineDiff(codeOf(old, k), codeOf(now, k)) : "") + (st.length ? "{" + st.map((s, i) => sm[i] + s.t).join("|") + "}" : ""); };
  const box = c => `${!old.classes[c] ? "+" : !now.classes[c] ? "-" : ""}${c}[${byClass[c].sort((a, b) => pattern.test(b.split(".")[1]) - pattern.test(a.split(".")[1]) || a.localeCompare(b)).map(leaf).join(", ")}]`;
  const order = Object.keys(byClass).sort((a, b) => (degree[b] || 0) - (degree[a] || 0) || a.localeCompare(b)); // busiest first = the center
  const folders = []; for(const c of order) if(!folders.includes(info(c).folder)) folders.push(info(c).folder);
  const boxes = folders.map(f => { const cs = order.filter(c => info(c).folder === f).map(box); return f ? `${f}/[\n  ${cs.join(",\n  ")}\n]` : cs.join("\n"); });
  return boxes.join("\n") + "\n\n" + parts.map(p => p.split("\n").flatMap(fromStep).join("\n")).join("\n\n");
}
const out = {}, every = {used:new Set(), parts:[], degree:{}};
for(const [step, pattern, when] of STEPS){
  const roots = [...new Set([...Object.keys(now.nodes), ...Object.keys(old.nodes)])].filter(k => pattern.test(k.split(".")[1])).sort();
  const perClass = {}; for(const r of roots) perClass[r.split(".")[0]] = (perClass[r.split(".")[0]] || 0) + 1;
  const parts = [], used = new Set(), degree = {}, stage = new Set();
  for(const r of roots){
    const a = old.nodes[r] ? reach(old, r) : [], b = now.nodes[r] ? reach(now, r) : [], lines = [];
    for(const e of b) lines.push((a.includes(e) ? "" : "+ ") + e);
    for(const e of a) if(!b.includes(e)) lines.push("- " + e);
    if(markOf(r)) used.add(r);
    if(!lines.length) continue;
    for(const e of [...a, ...b]) for(const k of e.split(" -> ")){ used.add(k); degree[k.split(".")[0]] = (degree[k.split(".")[0]] || 0) + 1; }
    parts.push(`# ${perClass[r.split(".")[0]] > 1 ? r : r.split(".")[0]}\n${lines.join("\n")}`);
    for(const l of lines) stage.add(l);
  }
  if(!used.size) continue;
  for(const k of used){ addCode(k); every.used.add(k); }
  for(const c in degree) every.degree[c] = (every.degree[c] || 0) + degree[c];
  if(stage.size) every.parts.push(`# ${step}\n${[...stage].join("\n")}`);
  out[step] = {when, src:source(pattern, used, parts, degree)};
}
out["All steps"] = {when:"Every Unity step in one graph. The parts are the steps.", src:source(/^(Awake|Start|Update|FixedUpdate|LateUpdate|On(Trigger|Collision|Enable|Disable|Destroy).*)$/, every.used, every.parts, every.degree)};

// every changed function, reached by a step or not
const changedFns = [...new Set([...Object.keys(now.nodes), ...Object.keys(old.nodes)])].filter(k => markOf(k)).sort();
for(const k of changedFns) addCode(k);
// what changed outside functions: usings, fields, enums, attributes... Each function is folded to its first line
const fold = (g, f) => { const t = g.files[f]; if(t == null) return "";
  let o = "", i = 0; for(const [a, b, head] of [...g.ranges[f]].sort((x, y) => x[0] - y[0])) if(a >= i){ o += t.slice(i, a) + head + " ⋯"; i = b; }
  return o + t.slice(i); };
const outside = {};
for(const f of [...new Set([...Object.keys(old.files), ...Object.keys(now.files)])].sort()){
  if(old.files[f] === now.files[f]) continue;
  const a = fold(old, f), b = fold(now, f);
  if(a.replace(/\s+/g, "") !== b.replace(/\s+/g, "")) outside[f] = {m:!old.files[f] ? "+" : !now.files[f] ? "-" : "~", d:diff(a, b)};
}

// other files that changed (not code): name and lines, no diff. .meta files are Unity's
const range = NEW === "WORK" ? [OLD] : [OLD, NEW], status = {};
for(const l of git("diff", "--name-status", "--no-renames", ...range).split("\n").filter(Boolean)){ const [st, f] = l.split("\t"); status[f] = st; }
const otherFiles = git("diff", "--numstat", "--no-renames", ...range).split("\n").filter(Boolean).map(l => l.split("\t"))
  .filter(([, , f]) => !/\.(cs|meta)$/.test(f)).map(([a, d, f]) => ({f, st:status[f] || "M", add:a, del:d}));

const subject = NEW === "WORK" ? "files on disk, not committed" : git("log", "-1", "--format=%s", NEW).trim();
const name = r => r === "WORK" ? "working files" : git("rev-parse", "--short", r).trim();
const esc = t => t.replace(/&/g, "&amp;").replace(/</g, "&lt;");
let page = fs.readFileSync(path.join(__dirname, "page.html"), "utf8");
const json = v => JSON.stringify(v).replace(/<\//g, "<\\/"); // no "</script>" inside the page's script
page = page.replace(/const STEPS = \{[\s\S]*?\n\};\n/, () => "const STEPS = " + json(out) + ";\n")
  .replace("const CODE = {}, CHANGED_FNS = [], OUTSIDE = {}, OTHER_FILES = [];", () => "const CODE = " + json(code) + ", CHANGED_FNS = " + json(changedFns) + ", OUTSIDE = " + json(outside) + ", OTHER_FILES = " + json(otherFiles) + ";")
  .replace(/<p class="commit">.*?<\/p>/, () => `<p class="commit"><b>${name(OLD)} → ${name(NEW)}</b> ${esc(subject)}</p>`);
process.stdout.write(page);
