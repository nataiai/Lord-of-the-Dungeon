"""C# source -> syntax model (types, members, statement trees), using tree-sitter.

Everything here is deterministic: same source in, same model out.
"""
from __future__ import annotations

import hashlib
import re
from dataclasses import dataclass, field

import tree_sitter_c_sharp as tscs
from tree_sitter import Language, Parser

_LANG = Language(tscs.language())
_PARSER = Parser(_LANG)

TYPE_DECLS = {
    "class_declaration": "class",
    "struct_declaration": "struct",
    "interface_declaration": "interface",
    "enum_declaration": "enum",
    "record_declaration": "record",
    "record_struct_declaration": "record struct",
}


def short_hash(text: str, n: int = 8) -> str:
    return hashlib.sha1(text.encode("utf8")).hexdigest()[:n]


# ----------------------------------------------------------------------------- data


@dataclass
class Param:
    name: str
    type: str
    mod: str = ""  # out / ref / in / params / this


@dataclass
class Stmt:
    """One statement. Compound statements keep their sub-blocks in `parts`."""

    kind: str  # simple|if|switch|loop|do|return|yield|yieldbreak|throw|break|continue|try|using|localfunc
    text: str  # what the node shows (the header for compound statements)
    line: int
    end: int
    comment: str = ""
    parts: list = field(default_factory=list)  # [(label, [Stmt])]
    node: object = None
    uses: list = field(default_factory=list)  # filled by resolve.py: [(kind, target_id)]
    status: str = "same"  # diff: same|added|removed|modified
    old_text: str | None = None  # diff: previous text when modified

    @property
    def sig(self) -> str:  # what must be equal for two statements to be "the same"
        return self.kind + ":" + re.sub(r"\s+", "", self.text)


@dataclass
class Member:
    kind: str  # field|const|property|method|ctor|accessor|event|enum_member|localfunc
    name: str
    type: str = ""
    line: int = 0
    end: int = 0
    mods: list = field(default_factory=list)
    attrs: list = field(default_factory=list)
    params: list = field(default_factory=list)
    init: str = ""
    comment: str = ""  # comment on the declaration line, or right above it
    tooltip: str = ""
    body: object = None  # ts node: block / arrow_expression_clause / expression
    accessors: dict = field(default_factory=dict)  # property: {'get': Member, 'set': Member}
    owner: str = ""  # full type name
    fingerprint: str = ""  # hash of the code without comments/whitespace
    stmts: list | None = None  # statement tree (methods / accessors with bodies)
    node: object = None

    @property
    def is_static(self) -> bool:
        return "static" in self.mods or self.kind == "const"


@dataclass
class TypeDecl:
    kind: str
    name: str
    full: str
    file: str
    line: int
    end: int
    bases: list = field(default_factory=list)
    mods: list = field(default_factory=list)
    attrs: list = field(default_factory=list)
    members: list = field(default_factory=list)
    outer: str | None = None
    comment: str = ""
    fingerprint: str = ""


@dataclass
class FileInfo:
    path: str
    lines: int
    code_lines: int
    comment_lines: int
    types: list = field(default_factory=list)
    usings: list = field(default_factory=list)
    fingerprint: str = ""
    external: bool = False


# ----------------------------------------------------------------------------- helpers


class Src:
    def __init__(self, data: bytes):
        self.data = data
        self.lines = data.decode("utf8", "replace").split("\n")
        self.eol_comment: dict[int, str] = {}  # line -> comment that follows code on that line
        self.own_line_comment: dict[int, str] = {}  # line -> comment alone on its line

    def text(self, n) -> str:
        return self.data[n.start_byte : n.end_byte].decode("utf8", "replace")


def _ln(n) -> int:
    return n.start_point[0] + 1


def _end(n) -> int:
    return n.end_point[0] + 1


def _named(n):
    return [c for c in n.children if c.is_named and c.type != "comment"]


def _clean_comment(t: str) -> str:
    t = t.strip()
    if t.startswith("//"):
        t = t[2:]
    elif t.startswith("/*"):
        t = t[2:-2]
    return t.strip().lstrip("/").strip()


_SPACE_OPS = {
    "binary_expression",
    "assignment_expression",
}


def fmt(src: Src, n) -> str:
    """Code text of a node with readable spacing (a, b / a == b / x = y), whitespace collapsed, comments dropped."""
    out = _fmt(src, n)
    out = re.sub(r"[ \t]+", " ", out).strip()
    out = re.sub(r"\(\s+", "(", out)
    out = re.sub(r"\s+\)", ")", out)
    out = re.sub(r"\s+;", ";", out)
    out = re.sub(r"\s+,", ",", out)
    return out


def _fmt(src: Src, n) -> str:
    if n.type == "comment":
        return ""
    kids = n.children
    if not kids:
        return src.text(n)
    parts = []
    prev = None
    for i, c in enumerate(kids):
        if c.type == "comment":
            continue
        gap = src.data[(prev.end_byte if prev else n.start_byte) : c.start_byte]
        sep = " " if re.search(rb"\s", gap) else ""
        tok = _fmt(src, c)
        if not c.is_named:
            t = c.type
            if t == "," or (t == ";" and n.type == "for_statement"):
                parts.append(t + " ")
                prev = c
                continue
            spaced = (
                (n.type in _SPACE_OPS and i == 1)
                or (n.type == "variable_declarator" and t == "=")
                or (n.type == "conditional_expression" and t in ("?", ":"))
                or (n.type in ("lambda_expression", "arrow_expression_clause") and t == "=>")
                or (n.type == "switch_expression_arm" and t == "=>")
            )
            if spaced:
                parts.append(" " + t + " ")
                prev = c
                continue
        parts.append(sep + tok)
        prev = c
    return "".join(parts)


def _attrs(src: Src, n) -> tuple[list, str]:
    names, tooltip = [], ""
    for c in n.children:
        if c.type == "attribute_list":
            for a in c.children:
                if a.type == "attribute":
                    nm = a.child_by_field_name("name")
                    name = src.text(nm).split(".")[-1] if nm else src.text(a)
                    names.append(name)
                    if name == "Tooltip":
                        m = re.search(r'"((?:[^"\\]|\\.)*)"', src.text(a))
                        tooltip = m.group(1) if m else ""
    return names, tooltip


def _mods(src: Src, n) -> list:
    return [src.text(c) for c in n.children if c.type == "modifier"]


def _decl_comment(src: Src, n) -> str:
    """Comment on the first line of a declaration (end of line) or the comment lines right above it."""
    first = _ln(n)
    # the header line may continue up to the opening brace; look at the first line only
    if first in src.eol_comment:
        return src.eol_comment[first]
    above = []
    ln = first - 1
    while ln in src.own_line_comment:
        above.insert(0, src.own_line_comment[ln])
        ln -= 1
    return " ".join(above)


def code_fingerprint(src: Src, n) -> str:
    """Hash of a node's code ignoring comments and whitespace: comment edits / reformatting don't count as changes."""
    toks = []

    def walk(x):
        if x.type == "comment":
            return
        if not x.children:
            toks.append(src.text(x))
        for c in x.children:
            walk(c)

    walk(n)
    return short_hash(" ".join(toks))


# ----------------------------------------------------------------------------- parsing


def parse_file(path: str, data: bytes) -> FileInfo:
    tree = _PARSER.parse(data)
    src = Src(data)
    root = tree.root_node

    # comment index, so statements and declarations can show the developer's own comments
    def index_comments(n):
        if n.type == "comment":
            ln = _ln(n)
            line_text = src.lines[ln - 1]
            before = line_text[: n.start_point[1]]
            if before.strip():
                src.eol_comment[ln] = _clean_comment(src.text(n))
            else:
                src.own_line_comment[ln] = _clean_comment(src.text(n))
        for c in n.children:
            index_comments(c)

    index_comments(root)

    code_lines = sum(1 for i, l in enumerate(src.lines, 1) if l.strip() and not (i in src.own_line_comment))
    fi = FileInfo(
        path=path,
        lines=len(src.lines) - (1 if src.lines and src.lines[-1] == "" else 0),
        code_lines=code_lines,
        comment_lines=len(src.own_line_comment),
        fingerprint=code_fingerprint(src, root),
    )
    fi._src = src  # type: ignore[attr-defined]

    def visit(n, outer: TypeDecl | None, ns: str):
        for c in n.children:
            if c.type in ("namespace_declaration",):
                name = src.text(c.child_by_field_name("name"))
                visit(c.child_by_field_name("body"), outer, (ns + "." if ns else "") + name)
            elif c.type == "file_scoped_namespace_declaration":
                visit(c, outer, src.text(c.child_by_field_name("name")))
            elif c.type in ("preproc_if", "preproc_else", "preproc_elif", "declaration_list"):
                visit(c, outer, ns)
            elif c.type in TYPE_DECLS:
                t = _type_decl(src, c, path, outer)
                fi.types.append(t)
                body = c.child_by_field_name("body")
                if body is not None:
                    _members(src, body, t)
                    visit(body, t, ns)
            elif c.type == "using_directive":
                fi.usings.append(fmt(src, c).replace("using ", "").rstrip(";"))
            elif c.type == "delegate_declaration" and outer is None:
                pass

    visit(root, None, "")
    return fi


def _type_decl(src: Src, n, path: str, outer: TypeDecl | None) -> TypeDecl:
    name = src.text(n.child_by_field_name("name"))
    bases = []
    for c in n.children:
        if c.type == "base_list":
            for b in _named(c):
                if b.type == "primary_constructor_base_type":
                    b = b.children[0]
                bases.append(fmt(src, b))
    attrs, _ = _attrs(src, n)
    t = TypeDecl(
        kind=TYPE_DECLS[n.type],
        name=name,
        full=(outer.full + "." + name) if outer else name,
        file=path,
        line=_ln(n),
        end=_end(n),
        bases=bases,
        mods=_mods(src, n),
        attrs=attrs,
        outer=outer.full if outer else None,
        comment=_decl_comment(src, n),
        fingerprint=code_fingerprint(src, n),
    )
    return t


def _params(src: Src, plist) -> list:
    out = []
    if plist is None:
        return out
    for p in _named(plist):
        if p.type != "parameter":
            continue
        tn = p.child_by_field_name("type")
        nm = p.child_by_field_name("name")
        mods = [src.text(c) for c in p.children if c.type in ("modifier", "parameter_modifier")]
        if not mods:
            mods = [src.text(c) for c in p.children if not c.is_named and src.text(c) in ("out", "ref", "in", "params", "this")]
        out.append(Param(name=src.text(nm) if nm else "", type=fmt(src, tn) if tn else "", mod=" ".join(mods)))
    return out


def _members(src: Src, body, t: TypeDecl):
    for c in body.children:
        if c.type == "preproc_if":
            _members(src, c, t)
            continue
        attrs, tooltip = _attrs(src, c)
        mods = _mods(src, c)
        if c.type in ("field_declaration", "event_field_declaration"):
            vd = next(x for x in c.children if x.type == "variable_declaration")
            ty = fmt(src, vd.child_by_field_name("type"))
            for d in vd.children:
                if d.type != "variable_declarator":
                    continue
                nm = src.text(d.child_by_field_name("name") or d.children[0])
                init = ""
                kids = [k for k in d.children if k.is_named]
                if len(kids) > 1:
                    init = fmt(src, kids[-1])
                kind = "const" if "const" in mods else ("event" if c.type == "event_field_declaration" else "field")
                m = Member(kind=kind, name=nm, type=ty, line=_ln(d), end=_end(d), mods=mods, attrs=attrs, init=init,
                           comment=_decl_comment(src, c), tooltip=tooltip, owner=t.full, node=c,
                           fingerprint=code_fingerprint(src, c))
                t.members.append(m)
        elif c.type == "property_declaration":
            ty = fmt(src, c.child_by_field_name("type"))
            nm = src.text(c.child_by_field_name("name"))
            m = Member(kind="property", name=nm, type=ty, line=_ln(c), end=_end(c), mods=mods, attrs=attrs,
                       comment=_decl_comment(src, c), tooltip=tooltip, owner=t.full, node=c,
                       fingerprint=code_fingerprint(src, c))
            val = c.child_by_field_name("value")
            acc = c.child_by_field_name("accessors")
            if val is not None and val.type == "arrow_expression_clause":
                m.accessors["get"] = _accessor(src, m, "get", val, val)
            if acc is not None:
                for a in acc.children:
                    if a.type != "accessor_declaration":
                        continue
                    kw = next((src.text(k) for k in a.children if not k.is_named and src.text(k) in ("get", "set", "init")), None)
                    if kw is None:
                        kw = src.text(a).split("=>")[0].split("{")[0].strip().split()[-1]
                    abody = a.child_by_field_name("body")
                    m.accessors[kw] = _accessor(src, m, kw, a, abody)
            if acc is not None:  # `{get;set;} = value;`
                init = next((x for x in c.children if x.is_named and x.type != "comment" and x.start_byte > acc.end_byte), None)
                if init is not None:
                    m.init = fmt(src, init)
            t.members.append(m)
        elif c.type in ("method_declaration", "constructor_declaration", "destructor_declaration", "operator_declaration",
                        "conversion_operator_declaration"):
            nm_node = c.child_by_field_name("name")
            nm = src.text(nm_node) if nm_node is not None else ("~" + t.name if c.type == "destructor_declaration" else "operator")
            if c.type == "constructor_declaration":
                nm = t.name
            ret = c.child_by_field_name("returns") or c.child_by_field_name("type")
            body_n = c.child_by_field_name("body")
            if body_n is None:  # expression-bodied: `=> expr;`
                body_n = next((x for x in c.children if x.type == "arrow_expression_clause"), None)
            m = Member(kind="ctor" if c.type == "constructor_declaration" else "method", name=nm,
                       type=fmt(src, ret) if ret is not None else "", line=_ln(c), end=_end(c), mods=mods, attrs=attrs,
                       params=_params(src, c.child_by_field_name("parameters")), comment=_decl_comment(src, c),
                       owner=t.full, body=body_n, node=c, fingerprint=code_fingerprint(src, c))
            if body_n is not None:
                m.stmts = stmt_tree(src, body_n)
            t.members.append(m)
        elif c.type == "enum_member_declaration_list":
            for e in c.children:
                if e.type == "enum_member_declaration":
                    nm = src.text(e.child_by_field_name("name"))
                    val = e.child_by_field_name("value")
                    t.members.append(Member(kind="enum_member", name=nm, init=fmt(src, val) if val else "", line=_ln(e),
                                            end=_end(e), owner=t.full, comment=src.eol_comment.get(_ln(e), ""),
                                            node=e, fingerprint=code_fingerprint(src, e)))
        elif c.type == "indexer_declaration":
            t.members.append(Member(kind="indexer", name="this[]", type=fmt(src, c.child_by_field_name("type")), line=_ln(c),
                                    end=_end(c), mods=mods, owner=t.full, node=c, fingerprint=code_fingerprint(src, c)))
    if t.kind == "enum":
        # tree-sitter puts enum members straight in the body
        for e in body.children:
            if e.type == "enum_member_declaration":
                nm = src.text(e.child_by_field_name("name"))
                val = e.child_by_field_name("value")
                if not any(m.name == nm for m in t.members):
                    t.members.append(Member(kind="enum_member", name=nm, init=fmt(src, val) if val else "", line=_ln(e),
                                            end=_end(e), owner=t.full, node=e, fingerprint=code_fingerprint(src, e)))


def _accessor(src: Src, prop: Member, kw: str, node, body) -> Member:
    a = Member(kind="accessor", name=f"{prop.name}.{kw}", type=prop.type, line=_ln(node), end=_end(node), owner=prop.owner,
               body=body, node=node, fingerprint=code_fingerprint(src, node))
    if body is not None:
        a.stmts = stmt_tree(src, body)
    return a


# ----------------------------------------------------------------------------- statement trees

LOOPS = {"for_statement", "foreach_statement", "while_statement"}


def stmt_tree(src: Src, body) -> list:
    if body.type == "block":
        return _stmts(src, _named(body))
    if body.type == "arrow_expression_clause":
        expr = _named(body)[0]
        return [Stmt("simple", fmt(src, expr), _ln(expr), _end(expr), _eol(src, expr), node=expr)]
    return _stmts(src, [body])


def _eol(src: Src, n) -> str:
    """End-of-line comments on the lines a statement covers (only lines the statement starts or ends on)."""
    got = []
    for ln in sorted({_ln(n), _end(n)}):
        c = src.eol_comment.get(ln)
        if c and c not in got:
            got.append(c)
    return " · ".join(got)


def _block(src: Src, n) -> list:
    if n is None:
        return []
    if n.type == "block":
        return _stmts(src, _named(n))
    return _stmts(src, [n])


def _stmts(src: Src, nodes) -> list:
    out = []
    for n in nodes:
        s = _stmt(src, n)
        if s is None:
            continue
        if isinstance(s, list):
            out.extend(s)
        else:
            out.append(s)
    return out


def _stmt(src: Src, n):
    t = n.type
    L, E = _ln(n), _end(n)
    if t == "block":
        return _stmts(src, _named(n))
    if t == "empty_statement":
        return None
    if t == "if_statement":
        cond = n.child_by_field_name("condition")
        cons = n.child_by_field_name("consequence")
        alt = n.child_by_field_name("alternative")
        parts = [("yes", _block(src, cons))]
        if alt is not None:
            parts.append(("no", _block(src, alt)))
        comment = src.eol_comment.get(L, "")
        for _, sub in parts:  # `if(x)return; // why` -> the comment belongs to the decision, once
            if sub and sub[0].line == L and sub[0].comment == comment and sub[0].kind != "simple":
                sub[0].comment = ""
        return Stmt("if", fmt(src, cond), L, E, comment, parts=parts, node=n)
    if t == "switch_statement":
        val = n.child_by_field_name("value")
        body = n.child_by_field_name("body")
        sections, pending_labels = [], []
        for sec in _named(body):
            if sec.type != "switch_section":
                continue
            labels, stmts = [], []
            for c in _named(sec):
                if c.type in ("constant_pattern", "declaration_pattern", "recursive_pattern", "type_pattern",
                              "relational_pattern", "or_pattern", "and_pattern", "not_pattern", "var_pattern",
                              "discard", "parenthesized_pattern", "case_switch_label", "case_pattern_switch_label",
                              "when_clause"):
                    labels.append(fmt(src, c))
                elif c.type == "default_switch_label":
                    labels.append("default")
                else:
                    stmts.append(c)
            if not labels and src.text(sec).lstrip().startswith("default"):
                labels = ["default"]
            if not stmts:  # `case A:` falling into the next section
                pending_labels += labels
                continue
            all_labels = pending_labels + labels
            pending_labels = []
            body_stmts = _stmts(src, stmts)
            if body_stmts and body_stmts[-1].kind == "break":
                body_stmts = body_stmts[:-1]
            name = "default" if all_labels == ["default"] else ", ".join(l for l in all_labels)
            sections.append((name, body_stmts))
        return Stmt("switch", "switch " + fmt(src, val), L, E, src.eol_comment.get(L, ""), parts=sections, node=n)
    if t in LOOPS:
        if t == "foreach_statement":
            left = n.child_by_field_name("left")
            right = n.child_by_field_name("right")
            ty = n.child_by_field_name("type")
            header = f"for each {fmt(src, left)} in {fmt(src, right)}"
            if ty is not None and src.text(ty) != "var":
                header = f"for each {fmt(src, ty)} {fmt(src, left)} in {fmt(src, right)}"
        elif t == "while_statement":
            header = "while " + fmt(src, n.child_by_field_name("condition"))
        else:  # for (init; condition; update)
            init = n.child_by_field_name("initializer")
            cond = n.child_by_field_name("condition")
            upd = [c for c in n.children_by_field_name("update")]
            header = "for ({}; {}; {})".format(fmt(src, init) if init is not None else "",
                                              fmt(src, cond) if cond is not None else "",
                                              ", ".join(fmt(src, u) for u in upd))
        body = n.child_by_field_name("body")
        return Stmt("loop", header, L, E, src.eol_comment.get(L, ""), parts=[("each", _block(src, body))], node=n)
    if t == "do_statement":
        body = n.child_by_field_name("body")
        cond = n.child_by_field_name("condition")
        return Stmt("do", "while " + fmt(src, cond), L, E, src.eol_comment.get(E, ""), parts=[("each", _block(src, body))], node=n)
    if t == "return_statement":
        kids = _named(n)
        return Stmt("return", "return " + fmt(src, kids[0]) if kids else "return", L, E, _eol(src, n), node=n)
    if t == "yield_statement":
        kids = _named(n)
        if not kids:
            return Stmt("yieldbreak", "yield break", L, E, _eol(src, n), node=n)
        return Stmt("yield", "yield " + fmt(src, kids[0]), L, E, _eol(src, n), node=n)
    if t == "throw_statement":
        return Stmt("throw", fmt(src, n).rstrip(";"), L, E, _eol(src, n), node=n)
    if t == "break_statement":
        return Stmt("break", "break", L, E, _eol(src, n), node=n)
    if t == "continue_statement":
        return Stmt("continue", "continue", L, E, _eol(src, n), node=n)
    if t == "try_statement":
        parts = [("try", _block(src, n.child_by_field_name("body")))]
        for c in n.children:
            if c.type == "catch_clause":
                decl = next((x for x in c.children if x.type == "catch_declaration"), None)
                body = c.child_by_field_name("body") or next(x for x in c.children if x.type == "block")
                parts.append(("catch " + (fmt(src, decl) if decl else ""), _block(src, body)))
            elif c.type == "finally_clause":
                parts.append(("finally", _block(src, next(x for x in c.children if x.type == "block"))))
        return Stmt("try", "try", L, E, parts=parts, node=n)
    if t in ("using_statement", "lock_statement", "fixed_statement", "checked_statement", "unsafe_statement"):
        body = n.child_by_field_name("body") or next((x for x in reversed(n.children) if x.type == "block"), None)
        header = src.data[n.start_byte : body.start_byte].decode() if body is not None else src.text(n)
        return Stmt("using", re.sub(r"\s+", " ", header.strip()), L, E, src.eol_comment.get(L, ""),
                    parts=[("body", _block(src, body))], node=n)
    if t == "local_function_statement":
        return Stmt("localfunc", fmt(src, n.child_by_field_name("name")), L, E, node=n)
    # expression_statement, local_declaration_statement, and anything else: a plain step
    text = fmt(src, n).rstrip(";").strip()
    return Stmt("simple", text, L, E, _eol(src, n), node=n)
