#!/usr/bin/env python3
"""Collects every ```mermaid block from documentation markdown into one HTML page for a render check.

usage: mermaid_gallery.py OUT.html PATH [PATH ...]

PATH is a .md file or a folder (searched recursively).
The built-in browser opens only files inside the session's folders, so write OUT.html as a dot-named
temporary file in the repository root (e.g. .mermaid-check-tmp.html) and delete it after the check.
Open it in the built-in browser, wait a few seconds, then read the result with javascript_tool:

    window.__mermaidReport   // {done, total, failed: [{id, source, error}]}

Everything except the pinned mermaid script is inline: the built-in browser opens file:// pages as data:,
so relative assets would not load. Failed blocks are also outlined in red on the page.
GitHub uses its own renderer, so a pass here is strong evidence but not a guarantee.
"""
import html
import json
import os
import re
import sys
import textwrap

MERMAID = 'https://cdn.jsdelivr.net/npm/mermaid@10.9.1/dist/mermaid.min.js'
# Blocks may be indented inside list items; the closing fence uses the same indentation.
BLOCK_RE = re.compile(r'^([ \t]*)```mermaid[ \t]*\n(.*?)^\1```', re.S | re.M)


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
    out, paths = sys.argv[1], sys.argv[2:]
    blocks = []
    for doc in collect(paths):
        text = open(doc, encoding='utf-8').read()
        for m in BLOCK_RE.finditer(text):
            line = text.count('\n', 0, m.start()) + 1
            blocks.append((f'{doc}:{line}', textwrap.dedent(m.group(2))))

    items = '\n'.join(
        f'<section><h2>{html.escape(src)}</h2><div class="d" id="d{i}"></div></section>'
        for i, (src, _) in enumerate(blocks))
    page = f'''<!doctype html>
<html><head><meta charset="utf-8"><title>Mermaid check</title>
<style>body{{font:13px sans-serif;margin:16px;background:#fff;color:#111}}
section{{border:1px solid #ddd;margin:12px 0;padding:8px}}section.fail{{border:2px solid #d00}}
h2{{font:12px monospace;margin:0 0 8px}}pre{{white-space:pre-wrap;color:#d00}}</style>
<script src="{MERMAID}"></script></head><body>
<p id="s">rendering {len(blocks)} blocks…</p>
{items}
<script>
const blocks = {json.dumps([b for _, b in blocks])};
const sources = {json.dumps([s for s, _ in blocks])};
window.__mermaidReport = {{done: false, total: blocks.length, failed: []}};
mermaid.initialize({{startOnLoad: false, securityLevel: 'strict'}});
(async () => {{
  for (let i = 0; i < blocks.length; i++) {{
    const el = document.getElementById('d' + i);
    try {{
      const {{svg}} = await mermaid.render('m' + i, blocks[i]);
      el.innerHTML = svg;
    }} catch (e) {{
      el.parentElement.classList.add('fail');
      el.innerHTML = '<pre></pre>';
      el.firstChild.textContent = String(e && e.message || e);
      window.__mermaidReport.failed.push({{id: i, source: sources[i], error: String(e && e.message || e)}});
    }}
  }}
  window.__mermaidReport.done = true;
  document.getElementById('s').textContent =
    `${{blocks.length}} blocks, ${{window.__mermaidReport.failed.length}} failed`;
}})();
</script></body></html>
'''
    with open(out, 'w', encoding='utf-8') as f:
        f.write(page)
    print(f'wrote {out} with {len(blocks)} blocks')


if __name__ == '__main__':
    main()
