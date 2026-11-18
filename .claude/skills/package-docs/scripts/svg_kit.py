#!/usr/bin/env python3
"""Small SVG drawing kit for package documentation figures.

Import it from a figure script kept in the scratchpad (not in the repository):

    import sys; sys.path.insert(0, "<repo>/.claude/skills/package-docs/scripts")
    from svg_kit import Svg

    s = Svg(980, 300, "Figure title")
    s.t(24, 32, "Figure title", 16, weight="bold")
    s.t(24, 52, "One-line subtitle", 12, "sub")
    s.r(40, 100, 88, 48, "lblue", "blue")
    s.l(40, 180, 400, 180, "ink", arrow="ink")
    s.save("<repo>/Packages/<package>/Documentation~/images/figure.svg")

Every figure gets a white rounded panel so it stays readable on GitHub's dark theme.
No <script>, foreignObject, external fonts or images: GitHub strips them.
Run `python3 svg_kit.py <out.svg>` to write a small sample figure.
"""
import html
import os
import sys

FONT = '"Noto Sans KR","Apple SD Gothic Neo","Malgun Gothic","Segoe UI",sans-serif'
MONO = '"JetBrains Mono","Cascadia Code",Consolas,monospace'

# Keep color meanings consistent across figures:
# blue = timeline/time, orange = being mixed now/caution, green = safe/committed,
# purple = output, red = cancelled/error, gray = inactive.
C = dict(ink="#1f2937", sub="#6b7280", grid="#e5e7eb", blue="#2563eb", lblue="#dbeafe", orange="#ea580c",
         lorange="#ffedd5", green="#16a34a", lgreen="#dcfce7", purple="#7c3aed", lpurple="#ede9fe",
         red="#dc2626", lred="#fee2e2", gray="#9ca3af", lgray="#f3f4f6", yellow="#ca8a04", lyellow="#fef9c3")


def _c(color):
    return C.get(color, color)


class Svg:
    """Accumulates SVG elements. Colors accept a palette key from C or any CSS color."""

    def __init__(s, w, h, title):
        s.w, s.h, s.out = w, h, []
        s.out.append(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}" font-family=\'{FONT}\' font-size="13">')
        s.out.append(f'<title>{html.escape(title)}</title>')
        s.out.append('<defs>'
                     + ''.join(f'<marker id="a{n}" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z" fill="{c}"/></marker>'
                               for n, c in C.items())
                     + '</defs>')
        s.out.append(f'<rect x="0.5" y="0.5" width="{w-1}" height="{h-1}" rx="10" fill="#ffffff" stroke="{C["grid"]}"/>')

    def t(s, x, y, txt, size=13, color="ink", anchor="start", weight="normal", mono=False, italic=False):
        """Text. anchor: start | middle | end."""
        fam = f' font-family=\'{MONO}\'' if mono else ''
        st = ' font-style="italic"' if italic else ''
        s.out.append(f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" fill="{_c(color)}" text-anchor="{anchor}" font-weight="{weight}"{fam}{st}>{html.escape(txt)}</text>')

    def l(s, x1, y1, x2, y2, color="ink", w=1.5, dash=None, arrow=None, start_arrow=False):
        """Line. arrow: palette key of the arrowhead color; start_arrow adds one at the start too."""
        d = f' stroke-dasharray="{dash}"' if dash else ''
        m = f' marker-end="url(#a{arrow})"' if arrow else ''
        ms = f' marker-start="url(#a{arrow})"' if (arrow and start_arrow) else ''
        s.out.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" stroke="{_c(color)}" stroke-width="{w}"{d}{m}{ms}/>')

    def r(s, x, y, w, h, fill="lgray", stroke="gray", rx=3, sw=1, dash=None, op=1):
        """Rectangle."""
        d = f' stroke-dasharray="{dash}"' if dash else ''
        s.out.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" rx="{rx}" fill="{_c(fill)}" stroke="{_c(stroke)}" stroke-width="{sw}" opacity="{op}"{d}/>')

    def p(s, pts, color="blue", w=2.2, dash=None, fill="none"):
        """Polyline through [(x, y), ...]."""
        d = f' stroke-dasharray="{dash}"' if dash else ''
        s.out.append(f'<polyline points="{" ".join(f"{x:.1f},{y:.1f}" for x, y in pts)}" fill="{_c(fill)}" stroke="{_c(color)}" stroke-width="{w}" stroke-linejoin="round"{d}/>')

    def c(s, x, y, rad=4, fill="blue", stroke="#ffffff", sw=1.5):
        """Circle (point marker)."""
        s.out.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{rad}" fill="{_c(fill)}" stroke="{_c(stroke)}" stroke-width="{sw}"/>')

    def diamond(s, x, y, rad=6, fill="orange"):
        """Diamond marker (events such as anchors or stamps)."""
        s.out.append(f'<path d="M{x},{y-rad} L{x+rad},{y} L{x},{y+rad} L{x-rad},{y} z" fill="{_c(fill)}" stroke="#fff" stroke-width="1.2"/>')

    def save(s, path):
        """Writes the figure; creates parent folders."""
        s.out.append('</svg>')
        os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
        with open(path, "w", encoding="utf-8") as f:
            f.write("\n".join(s.out) + "\n")
        print("wrote", path)


def _sample(path):
    """Block strip with a marked current block: the layout used by the FMOD mix-block figure."""
    s = Svg(720, 200, "샘플: 블록 띠")
    s.t(24, 32, "샘플: 블록 띠", 16, weight="bold")
    s.t(24, 52, "한 칸 = 한 블록. 주황 = 지금 처리 중, 초록 = 확정됨", 12, "sub")
    x0, bw, y = 40, 80, 90
    for i in range(8):
        fill, st = ("lorange", "orange") if i == 4 else (("lgreen", "green") if i < 4 else ("lgray", "gray"))
        s.r(x0 + i * bw, y, bw - 4, 44, fill, st)
        s.t(x0 + i * bw + bw / 2 - 2, y + 27, f"k{i - 4:+d}" if i != 4 else "k", 12, "ink", "middle", mono=True)
    s.l(x0, y + 70, x0 + 8 * bw, y + 70, "ink", arrow="ink")
    s.t(x0 + 8 * bw, y + 90, "DSP 클록", 12, "sub", "end")
    s.save(path)


if __name__ == "__main__":
    _sample(sys.argv[1] if len(sys.argv) > 1 else "sample.svg")
