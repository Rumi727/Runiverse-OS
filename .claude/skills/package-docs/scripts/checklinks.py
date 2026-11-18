#!/usr/bin/env python3
"""Checks relative links in documentation markdown.

usage: checklinks.py [--ignore-word WORD ...] PATH [PATH ...]

PATH is a .md file or a folder; folders are searched recursively for *.md.
For every non-http link it checks:
  MISSING  the target file does not exist
  ANCHOR   a #heading anchor does not match a heading in the target .md (GitHub slug rules)
  BADLINE  a #L<n> anchor points past the end of the file
  SYMBOL?  no identifier from the link label appears on lines n..n+2 of the target
           (false positive when the label is only a file name: check by hand,
           or pass words that are not symbols with --ignore-word)
Links inside fenced code blocks are skipped.
"""
import argparse
import os
import re
import urllib.parse

LINK_RE = re.compile(r'\[([^\]]*)\]\(([^)\s]+)\)')
DEFAULT_IGNORE = {'cs', 'md', 'L', 'Runtime', 'Editor'}


def slug(h):
    h = h.strip().lower()
    h = re.sub(r'[`*_~]', '', h)
    h = re.sub(r'\[([^\]]*)\]\([^)]*\)', r'\1', h)
    h = re.sub(r'[^\w\- ]', '', h)
    return h.replace(' ', '-')


_heading_cache = {}


def headings(path):
    if path in _heading_cache:
        return _heading_cache[path]
    out, seen, in_code = set(), {}, False
    for line in open(path, encoding='utf-8'):
        if line.startswith('```'):
            in_code = not in_code
            continue
        if in_code:
            continue
        m = re.match(r'^(#{1,6})\s+(.*)', line)
        if m:
            s = slug(m.group(2))
            n = seen.get(s, 0)
            out.add(s if n == 0 else f'{s}-{n}')
            seen[s] = n + 1
    _heading_cache[path] = out
    return out


def collect(paths):
    docs = []
    for p in paths:
        if os.path.isdir(p):
            for d, _, files in os.walk(p):
                docs += [os.path.join(d, f) for f in files if f.endswith('.md')]
        else:
            docs.append(p)
    return sorted(set(docs))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('paths', nargs='+')
    ap.add_argument('--ignore-word', action='append', default=[])
    args = ap.parse_args()
    ignore = DEFAULT_IGNORE | set(args.ignore_word)

    problems = checked = 0
    for doc in collect(args.paths):
        text = re.sub(r'```.*?```', '', open(doc, encoding='utf-8').read(), flags=re.S)
        # Inline code spans that contain a whole link are examples, not links. Spans are paired left to right,
        # so a label such as [`Foo.cs#L1`](...) keeps its backticks and is still checked.
        text = re.sub(r'(`+)(.+?)\1', lambda s: '' if '](' in s.group(2) else s.group(0), text)
        for m in LINK_RE.finditer(text):
            label, target = m.group(1), m.group(2)
            if target.startswith(('http://', 'https://', 'mailto:')):
                continue
            path, _, frag = target.partition('#')
            path = urllib.parse.unquote(path)
            full = os.path.normpath(os.path.join(os.path.dirname(doc), path)) if path else doc
            checked += 1
            if not os.path.exists(full):
                print(f'MISSING  {doc} -> {target}')
                problems += 1
                continue
            if not frag:
                continue
            lm = re.fullmatch(r'L(\d+)(?:-L\d+)?', frag)
            if lm:
                n = int(lm.group(1))
                lines = open(full, encoding='utf-8').read().split('\n')
                if n > len(lines):
                    print(f'BADLINE  {doc} -> {target} (file has {len(lines)} lines)')
                    problems += 1
                    continue
                words = [w for w in re.findall(r'[A-Za-z_][A-Za-z0-9_]*', label)
                         if w not in ignore and not re.fullmatch(r'L\d+', w)]
                ctx = '\n'.join(lines[n - 1:n + 2])
                if words and not any(w in ctx for w in words):
                    print(f'SYMBOL?  {doc} -> {target} label="{label}" line="{lines[n - 1].strip()[:90]}"')
                    problems += 1
            elif full.endswith('.md') and urllib.parse.unquote(frag) not in headings(full):
                print(f'ANCHOR   {doc} -> {target}')
                problems += 1
    print(f'checked {checked} links, {problems} problems')


if __name__ == '__main__':
    main()
