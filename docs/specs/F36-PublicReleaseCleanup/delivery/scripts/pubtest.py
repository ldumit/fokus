"""Probes for publish-only-map.txt: each case's input must come out as its expected bytes, LF and CRLF,
and a second pass over the output must change nothing.

usage: python pubtest.py
"""
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
MAP = os.path.join(HERE, '..', 'publish-only-map.txt')
APPLY = os.path.join(HERE, '..', 'apply-map.py')
NOTE = b' (not included in the public release)'

CASES = {
    'no-separator': (b'### Tier 13 \xe2\x80\x94 Public Release\n\n| ID | F |\n| F36 | x |\n\n**F36:** para\n\n### Tier 14\n\n| F40 | keep |\n\n---\n\nend\n',
                     b'### Tier 13 \xe2\x80\x94 Public Release\n\n| ID | F |\n\n### Tier 14\n\n| F40 | keep |\n\n---\n\nend\n'),
    'eof-row': (b'a\n| F36 | x |', b'a\n'),
    'eof-para': (b'a\n**F36:** p', b'a\n'),
    'evaluation-path': (b'None (ad-hoc infrastructure \xe2\x80\x94 based on `docs/evaluations/cross-project-skill-mining.md`)\n',
                        b'None (ad-hoc infrastructure \xe2\x80\x94 based on `docs/evaluations/cross-project-skill-mining.md`' + NOTE + b')\n'),
    'source-line': (b'- **Source:** adhoc-SuiteScaffolding lessons\n',
                    b'- **Source:** adhoc-SuiteScaffolding lessons' + NOTE + b'\n'),
    'table-cell': (b'| 2+ (adhoc-SuiteScaffolding, implied others) | adhoc-SuiteScaffolding |\n',
                   b'| 2+ (adhoc-SuiteScaffolding, implied others) | adhoc-SuiteScaffolding' + NOTE + b' |\n'),
    'summary-list': (b'seeded from real lessons (F28-F32, adhoc-SuiteScaffolding), not hypothetical\n',
                     b'seeded from real lessons (F28-F32, adhoc-SuiteScaffolding' + NOTE + b'), not hypothetical\n'),
    'implementation-list': (b'(6 entries from F28, F29, F30, F31, F32, adhoc-SuiteScaffolding)\n',
                            b'(6 entries from F28, F29, F30, F31, F32, adhoc-SuiteScaffolding' + NOTE + b')\n'),
}


def apply(path):
    return subprocess.run([sys.executable, APPLY, '--stdout', MAP, path], capture_output=True).stdout


def main():
    tmp = tempfile.mkdtemp(prefix='f36-pubtest-')
    ok = True
    for name, (src, want) in CASES.items():
        for eol in ('lf', 'crlf'):
            s = src if eol == 'lf' else src.replace(b'\n', b'\r\n')
            w = want if eol == 'lf' else want.replace(b'\n', b'\r\n')
            p = os.path.join(tmp, f'{name}-{eol}.md')
            with open(p, 'wb') as f:
                f.write(s)
            out = apply(p)
            with open(p, 'wb') as f:
                f.write(out)
            again = apply(p)
            good = out == w and again == out
            ok &= good
            print(name, eol, 'ok' if good else f'MISMATCH {out!r} / second pass {again!r}')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
