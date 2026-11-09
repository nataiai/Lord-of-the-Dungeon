"""Symbol table + light type inference: which project members each statement calls, reads and writes.

Not a compiler: it follows declared types of fields, properties, locals, parameters, `out`/pattern variables,
foreach variables, `var` initializers, generic Unity helpers (GetComponent<T>, Instantiate(x) ...) and base classes.
Anything it can't follow (Unity API, packages) is simply not linked.
"""
from __future__ import annotations

import fnmatch
import re
from dataclasses import dataclass

from .csparse import FileInfo, Member, Stmt, TypeDecl

# Unity calls these itself; they are where behaviour starts
UNITY_EVENTS = {
    "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy", "OnValidate", "Reset",
    "OnTriggerEnter2D", "OnTriggerStay2D", "OnTriggerExit2D", "OnCollisionEnter2D", "OnCollisionStay2D", "OnCollisionExit2D",
    "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit", "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
    "OnDrawGizmos", "OnDrawGizmosSelected", "OnGUI", "OnApplicationQuit", "OnApplicationPause", "OnApplicationFocus",
    "OnBecameVisible", "OnBecameInvisible", "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseExit", "OnMouseOver",
    "OnAnimatorMove", "OnAnimatorIK", "OnRenderObject", "OnPreRender", "OnPostRender", "OnTransformParentChanged",
}
UNITY_BASES = {"MonoBehaviour", "NetworkBehaviour", "StateMachineBehaviour"}
GENERIC_T = {"GetComponent", "GetComponentInChildren", "GetComponentInParent", "AddComponent", "FindFirstObjectByType",
             "FindAnyObjectByType", "FindObjectOfType"}
GENERIC_T_ARRAY = {"GetComponents", "GetComponentsInChildren", "GetComponentsInParent", "FindObjectsByType",
                   "FindObjectsOfType"}
COLLECTIONS_1 = {"List", "HashSet", "Queue", "Stack", "IEnumerable", "IList", "ICollection", "IReadOnlyList",
                 "LinkedList", "SortedSet", "ConcurrentQueue", "IReadOnlyCollection"}
COMPONENT_PROPS = {"gameObject": "GameObject", "transform": "Transform"}


@dataclass(frozen=True)
class StaticRef:  # an expression that names a type (`GameManager` in `GameManager.Instance`)
    full: str


def strip_generic(t: str) -> str:
    return re.sub(r"<.*>$", "", t.strip().rstrip("?")).strip()


def generic_args(t: str) -> list:
    t = t.strip()
    m = re.match(r"^[\w.]+<(.*)>$", t)
    if not m:
        return []
    args, depth, cur = [], 0, ""
    for ch in m.group(1):
        if ch == "<":
            depth += 1
        elif ch == ">":
            depth -= 1
        if ch == "," and depth == 0:
            args.append(cur.strip())
            cur = ""
        else:
            cur += ch
    if cur.strip():
        args.append(cur.strip())
    return args


def elem_type(t: str | None) -> str | None:
    if not t:
        return None
    t = t.strip()
    if t.endswith("[]"):
        return t[:-2]
    base, args = strip_generic(t), generic_args(t)
    if base in COLLECTIONS_1 and len(args) == 1:
        return args[0]
    if base in ("Dictionary", "IDictionary", "IReadOnlyDictionary", "SortedDictionary") and len(args) == 2:
        return f"KeyValuePair<{args[0]}, {args[1]}>"
    return None


def index_type(t: str | None) -> str | None:
    if not t:
        return None
    base, args = strip_generic(t), generic_args(t)
    if base in ("Dictionary", "IDictionary", "IReadOnlyDictionary", "SortedDictionary") and len(args) == 2:
        return args[1]
    return elem_type(t)


class Project:
    def __init__(self, files: list[FileInfo], external_globs: list[str]):
        self.files = {f.path: f for f in files}
        self.external_globs = external_globs
        self.types: dict[str, TypeDecl] = {}
        self.by_simple: dict[str, list[TypeDecl]] = {}
        for f in files:
            f.external = self.path_is_external(f.path)
            for t in f.types:
                self.types[t.full] = t
                self.by_simple.setdefault(t.name, []).append(t)
        self.ids: dict[int, str] = {}  # id(Member) -> member id
        self.member_by_id: dict[str, Member] = {}
        for t in self.types.values():
            names = {}
            for m in t.members:
                if m.kind in ("method", "ctor"):
                    names[m.name] = names.get(m.name, 0) + 1
            for m in t.members:
                if m.kind in ("method", "ctor") and names[m.name] > 1:
                    mid = f"{t.full}.{m.name}({','.join(p.type for p in m.params)})"
                else:
                    mid = f"{t.full}.{m.name}"
                self.ids[id(m)] = mid
                self.member_by_id[mid] = m
                for k, a in m.accessors.items():
                    aid = f"{t.full}.{m.name}.{k}"
                    self.ids[id(a)] = aid
                    self.member_by_id[aid] = a

    # ------------------------------------------------------------------ lookup
    def path_is_external(self, path: str) -> bool:
        return any(fnmatch.fnmatch(path, g) or fnmatch.fnmatch(path.lower(), g.lower()) for g in self.external_globs)

    def is_external_type(self, full: str) -> bool:
        t = self.types.get(full)
        return t is None or self.path_is_external(t.file)

    def mid(self, m: Member) -> str:
        return self.ids[id(m)]

    def find_type(self, name: str | None, ctx: TypeDecl | None = None) -> TypeDecl | None:
        if not name:
            return None
        name = strip_generic(name)
        if name.endswith("[]"):
            return None
        if name in self.types:
            return self.types[name]
        # nested types visible from ctx: ctx.Name, outer.Name, ...
        c = ctx
        while c is not None:
            cand = self.types.get(c.full + "." + name)
            if cand:
                return cand
            c = self.types.get(c.outer) if c.outer else None
        # through base classes' nested types
        if ctx is not None:
            for b in self.base_chain(ctx)[1:]:
                cand = self.types.get(b.full + "." + name)
                if cand:
                    return cand
        cands = self.by_simple.get(name.split(".")[-1], [])
        return cands[0] if len(cands) == 1 else None

    def base_chain(self, t: TypeDecl) -> list:
        out, seen = [], set()
        while t is not None and t.full not in seen:
            out.append(t)
            seen.add(t.full)
            nxt = None
            for b in t.bases:
                bt = self.find_type(b, self.types.get(t.outer) if t.outer else None)
                if bt is not None and bt.kind in ("class", "record", "struct"):
                    nxt = bt
                    break
            t = nxt
        return out

    def external_base(self, t: TypeDecl) -> str | None:
        """The engine/library class at the root of t's inheritance (MonoBehaviour, ScriptableObject, ...)."""
        chain = self.base_chain(t)
        last = chain[-1]
        for b in last.bases:
            if self.find_type(b) is None:
                return strip_generic(b)
        return None

    def is_unity_component(self, t: TypeDecl) -> bool:
        return self.external_base(t) in UNITY_BASES

    def find_members(self, t: TypeDecl, name: str) -> list:
        for c in self.base_chain(t):
            ms = [m for m in c.members if m.name == name and m.kind not in ("accessor",)]
            if ms:
                return ms
        return []

    def pick_overload(self, ms: list, nargs: int | None) -> Member | None:
        ms = [m for m in ms if m.kind in ("method", "ctor")] or ms
        if not ms:
            return None
        if nargs is None or len(ms) == 1:
            return ms[0]
        for m in ms:
            need = sum(1 for p in m.params if "=" not in p.name and p.mod != "params")
            if need <= nargs <= len(m.params) or (m.params and m.params[-1].mod == "params" and nargs >= need):
                return m
        return ms[0]


class Resolver:
    """Walks statement trees in source order, keeping a scope of local variable types."""

    def __init__(self, project: Project):
        self.p = project

    def annotate_member(self, t: TypeDecl, m: Member, src) -> None:
        self.src = src
        self.ctx = t
        self.member = m
        self.scope: dict[str, str] = {p.name: p.type for p in m.params}
        self.local_funcs: dict[str, str] = {}
        if m.stmts:
            self._walk_stmts(m.stmts)
        for k, a in m.accessors.items():
            self.scope = {"value": m.type} if k in ("set", "init") else {}
            if a.stmts:
                self._walk_stmts(a.stmts)

    # ------------------------------------------------------------------ statements
    def _walk_stmts(self, stmts):
        for s in stmts:
            self.cur = s
            n = s.node
            if s.kind == "simple":
                self._expr(n)
            elif s.kind == "if":
                self._expr(n.child_by_field_name("condition"))
            elif s.kind == "switch":
                self._expr(n.child_by_field_name("value"))
            elif s.kind == "loop":
                if n.type == "foreach_statement":
                    right = n.child_by_field_name("right")
                    rt = self._expr(right)
                    left = n.child_by_field_name("left")
                    ty = n.child_by_field_name("type")
                    declared = self.src.text(ty) if ty is not None else "var"
                    et = elem_type(rt if isinstance(rt, str) else None)
                    if left is not None and left.type == "identifier":
                        self.scope[self.src.text(left)] = et if declared == "var" else declared
                elif n.type == "while_statement":
                    self._expr(n.child_by_field_name("condition"))
                else:  # for: initializer, condition, updates
                    body = n.child_by_field_name("body")
                    for c in n.children:
                        if c.is_named and c != body:
                            self._expr(c)
            elif s.kind == "do":
                self._expr(n.child_by_field_name("condition"))
            elif s.kind in ("return", "yield", "throw"):
                for c in n.children:
                    if c.is_named:
                        self._expr(c)
            elif s.kind == "using":
                body = n.child_by_field_name("body")
                for c in n.children:
                    if c.is_named and c != body and c.type != "block":
                        self._expr(c)
            elif s.kind == "localfunc":
                ret = n.child_by_field_name("type") or n.child_by_field_name("returns")
                self.local_funcs[s.text] = self.src.text(ret) if ret is not None else ""
            for _, sub in s.parts:
                self._walk_stmts(sub)

    def _use(self, kind: str, m: Member | None):
        if m is None:
            return
        mid = self.p.mid(m)
        if (kind, mid) not in self.cur.uses:
            self.cur.uses.append((kind, mid))

    # ------------------------------------------------------------------ expressions
    def _expr(self, n, write: bool = False):
        """Returns the expression's type (a type text, StaticRef, or None) and records uses."""
        if n is None:
            return None
        t = n.type
        T = self.src.text
        if t == "identifier":
            name = T(n)
            if name in self.scope:
                return self.scope[name]
            ms = self.p.find_members(self.ctx, name)
            if ms:
                m = ms[0]
                if m.kind in ("field", "const", "event"):
                    self._use("set" if write else "get", m)
                    return m.type
                if m.kind == "property":
                    self._prop_use(m, write)
                    return m.type
                if m.kind == "method":
                    self._use("ref", m)  # method group, e.g. `x += OnChatMessage`
                    return None
            ty = self.p.find_type(name, self.ctx)
            if ty is not None:
                return StaticRef(ty.full)
            return StaticRef(name) if name[:1].isupper() else None
        if t in ("this_expression", "this"):
            return self.ctx.full
        if t in ("base_expression", "base"):
            chain = self.p.base_chain(self.ctx)
            return chain[1].full if len(chain) > 1 else None
        if t == "member_access_expression":
            recv = n.child_by_field_name("expression")
            name_n = n.child_by_field_name("name")
            rt = self._expr(recv)
            return self._member(rt, T(name_n), write, None)
        if t == "invocation_expression":
            fn = n.child_by_field_name("function")
            args = n.child_by_field_name("arguments")
            argn = [a for a in (args.children if args else []) if a.type == "argument"]
            argtypes = [self._expr(next((c for c in a.children if c.is_named), None)) for a in argn]
            return self._call(fn, argn, argtypes)
        if t == "generic_name":
            ty = self.p.find_type(T(n), self.ctx)
            return StaticRef(ty.full) if ty else None
        if t == "element_access_expression":
            rt = self._expr(n.child_by_field_name("expression"))
            for c in n.children:
                if c.type == "bracketed_argument_list":
                    self._expr(c)
            return index_type(rt if isinstance(rt, str) else None)
        if t == "assignment_expression":
            left = n.child_by_field_name("left")
            right = n.child_by_field_name("right")
            rt = self._expr(right)
            op = n.child_by_field_name("operator")
            compound = op is not None and T(op) != "="
            if compound:
                self._expr(left)  # a compound assignment reads first
            lt = self._expr(left, write=True)
            return lt or rt
        if t in ("prefix_unary_expression", "postfix_unary_expression"):
            op = next((T(c) for c in n.children if not c.is_named), "")
            inner = next((c for c in n.children if c.is_named), None)
            if op in ("++", "--"):
                self._expr(inner)
                return self._expr(inner, write=True)
            return self._expr(inner)
        if t == "object_creation_expression":
            ty = n.child_by_field_name("type")
            for c in n.children:
                if c.is_named and c != ty:
                    self._expr(c)
            tt = self.p.find_type(T(ty), self.ctx) if ty is not None else None
            if tt is not None:
                ctor = next((m for m in tt.members if m.kind == "ctor"), None)
                if ctor:
                    self._use("call", ctor)
            return T(ty) if ty is not None else None
        if t == "cast_expression":
            self._expr(n.child_by_field_name("value"))
            return T(n.child_by_field_name("type"))
        if t == "conditional_expression":
            self._expr(n.child_by_field_name("condition"))
            a = self._expr(n.child_by_field_name("consequence"))
            b = self._expr(n.child_by_field_name("alternative"))
            return a if isinstance(a, str) else b
        if t == "binary_expression":
            left = n.child_by_field_name("left")
            right = n.child_by_field_name("right")
            op = n.child_by_field_name("operator")
            lt = self._expr(left)
            if op is not None and T(op) == "as":
                return T(right)
            rt = self._expr(right)
            if op is not None and T(op) == "??":
                return lt or rt
            return lt if T(op) in ("+", "-", "*", "/") else "bool"
        if t == "is_pattern_expression":
            self._expr(n.child_by_field_name("expression"))
            self._pattern(n.child_by_field_name("pattern"))
            return "bool"
        if t == "declaration_expression":  # `out Character c`
            ty = n.child_by_field_name("type")
            nm = n.child_by_field_name("name")
            if nm is not None and ty is not None and T(ty) != "var":
                self.scope[T(nm)] = T(ty)
            return T(ty) if ty is not None else None
        if t == "variable_declaration":
            ty = n.child_by_field_name("type")
            declared = T(ty) if ty is not None else "var"
            for d in n.children:
                if d.type == "variable_declarator":
                    nm = d.child_by_field_name("name") or d.children[0]
                    vals = [k for k in d.children if k.is_named][1:]
                    vt = self._expr(vals[-1]) if vals else None
                    self.scope[T(nm)] = (vt if isinstance(vt, str) else None) if declared == "var" else declared
            return None
        if t == "switch_expression":
            self._expr(n.child_by_field_name("value") or n.children[0])
            out = None
            for arm in n.children:
                if arm.type == "switch_expression_arm":
                    kids = [c for c in arm.children if c.is_named]
                    if kids:
                        self._pattern(kids[0])
                    for k in kids[1:]:
                        r = self._expr(k)
                        out = out or (r if isinstance(r, str) else None)
            return out
        if t == "lambda_expression":
            saved = dict(self.scope)
            for c in n.children:
                if c.is_named:
                    self._expr(c)
            self.scope = saved
            return None
        if t in ("parenthesized_expression", "argument", "expression_statement", "local_declaration_statement",
                 "arrow_expression_clause", "equals_value_clause", "bracketed_argument_list", "argument_list",
                 "interpolated_string_expression", "interpolation", "tuple_expression", "initializer_expression",
                 "array_creation_expression", "implicit_array_creation_expression", "checked_expression",
                 "await_expression", "throw_expression", "range_expression", "ref_expression",
                 "implicit_object_creation_expression", "anonymous_object_creation_expression", "with_expression",
                 "conditional_access_expression", "member_binding_expression", "element_binding_expression",
                 "typeof_expression", "sizeof_expression", "default_expression"):
            last = None
            for c in n.children:
                if c.is_named:
                    last = self._expr(c, write if t == "parenthesized_expression" else False)
            if t == "parenthesized_expression":
                return last
            if t == "array_creation_expression":
                ty = n.child_by_field_name("type")
                return T(ty) if ty is not None else None
            if t == "typeof_expression":
                return "Type"
            return None
        if t.endswith("_literal") or t in ("string_literal", "verbatim_string_literal", "raw_string_literal"):
            return {"integer_literal": "int", "real_literal": "float", "boolean_literal": "bool", "null_literal": None,
                    "character_literal": "char"}.get(t, "string")
        for c in n.children:  # anything else: still look inside for uses
            if c.is_named:
                self._expr(c)
        return None

    def _pattern(self, p):
        if p is None:
            return
        T = self.src.text
        if p.type == "declaration_pattern":
            ty = p.child_by_field_name("type")
            nm = p.child_by_field_name("name")
            if ty is not None and nm is not None:
                self.scope[T(nm)] = T(ty)
        for c in p.children:
            if c.is_named and c.type.endswith("pattern"):
                self._pattern(c)

    def _prop_use(self, m: Member, write: bool):
        # model.py turns this into a call when the accessor runs project code (e.g. Player.HP.set updates the UI)
        self._use("set" if write else "get", m)

    def _member(self, rt, name: str, write: bool, nargs: int | None, invoked: bool = False):
        """Resolve `<receiver of type rt>.name`."""
        if name in COMPONENT_PROPS and not isinstance(rt, StaticRef):
            return COMPONENT_PROPS[name]
        ty = None
        if isinstance(rt, StaticRef):
            ty = self.p.types.get(rt.full)
        elif isinstance(rt, str):
            ty = self.p.find_type(rt, self.ctx)
        if ty is None:
            return None
        bare = re.sub(r"<.*>$", "", name)
        ms = self.p.find_members(ty, bare)
        if not ms:
            nested = self.p.types.get(ty.full + "." + bare)
            return StaticRef(nested.full) if nested else None
        m = self.p.pick_overload(ms, nargs) if invoked else ms[0]
        if m.kind in ("method", "ctor"):
            self._use("call" if invoked else "ref", m)
            return m.type
        if m.kind == "property":
            self._prop_use(m, write)
            return m.type
        if m.kind == "enum_member":
            self._use("get", m)
            return ty.full
        self._use("set" if write else "get", m)
        return m.type

    def _call(self, fn, argn, argtypes):
        T = self.src.text
        nargs = len(argn)
        # Generic Unity helpers: GetComponent<T>() -> T
        name_n = fn.child_by_field_name("name") if fn.type == "member_access_expression" else fn
        recv_t = None
        if fn.type == "member_access_expression":
            recv_t = self._expr(fn.child_by_field_name("expression"))
        if name_n is not None and name_n.type == "generic_name":
            gname = T(name_n.children[0])
            targs = generic_args(T(name_n))
            if gname in GENERIC_T and targs:
                tt = self.p.find_type(targs[0], self.ctx)
                if tt is not None and gname == "AddComponent":
                    self.cur.uses.append(("new", tt.full)) if ("new", tt.full) not in self.cur.uses else None
                return targs[0]
            if gname in GENERIC_T_ARRAY and targs:
                return targs[0] + "[]"
        fname = T(name_n) if name_n is not None else ""
        if fname == "Instantiate":
            first = argtypes[0] if argtypes else None
            if isinstance(first, str):
                tt = self.p.find_type(first, self.ctx)
                if tt is not None:
                    self.cur.uses.append(("new", tt.full)) if ("new", tt.full) not in self.cur.uses else None
                return first
            return None
        if fn.type == "identifier":
            if fname in self.local_funcs:
                return self.local_funcs[fname]
            ms = self.p.find_members(self.ctx, fname)
            if ms:
                m = self.p.pick_overload(ms, nargs)
                self._use("call", m)
                return m.type
            return None
        if fn.type == "member_access_expression":
            recv = fn.child_by_field_name("expression")
            if recv is not None and recv.type in ("base_expression", "base"):
                # base.X(): runs the base implementation only (never another override)
                before = len(self.cur.uses)
                r = self._member(recv_t, fname, False, nargs, invoked=True)
                self.cur.uses[before:] = [("base" if k == "call" else k, t) for k, t in self.cur.uses[before:]]
                return r
            return self._member(recv_t, fname, False, nargs, invoked=True)
        self._expr(fn)
        return None


