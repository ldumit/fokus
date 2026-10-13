"""Step 7 history replay: every added line in kept history, redacted by the shipped applier, has no deny hit.

usage: python replay.py [scratch-dir]

Mirrors filter-repo: an added line belongs to the file on its `+++ b/` header, files under an
exclusions.txt entry are dropped by filter-repo's prefix rule, and the remaining lines are written to
one scratch file and redacted by `apply-map.py --stdout`. Prints lines checked, hits before, hits after.
"""
import os
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = subprocess.run(['git', 'rev-parse', '--show-toplevel'], cwd=HERE, capture_output=True,
                      check=True, text=True).stdout.strip()
D = 'docs/specs/F36-PublicReleaseCleanup/delivery'
DENY = re.compile(rb'(?i)omnishelf|omniaz|alfamart|\bPD-[0-9]{3,5}\b|project PD\b|shelf.compliance|sprint-rituals'
                  rb'|SprintRituals|Reflekt|rituals suite|dotnet-microservices|code-forge|C:/Users/Laurentiu'
                  rb'|D:[/\\]src|\b(Taylor|Mateusz|Adam|Pawel|Pawe\xc5\x82|Ovidiu|Norbert|Calin|Maciej)\b')


def main(out_dir):
    os.chdir(REPO)
    with open(f'{D}/exclusions.txt', 'rb') as f:
        excl = [line.strip() for line in f.read().splitlines() if line.strip()]

    def excluded(path):
        return any(path.startswith(e) and (e.endswith(b'/') or len(path) == len(e) or path[len(e):len(e) + 1] == b'/')
                   for e in excl)

    log = subprocess.run(['git', 'log', '--all', '-p', '--format=', '--', '.', ':(exclude)client/package-lock.json'],
                         capture_output=True, check=True).stdout
    added = []
    path = None
    for line in log.split(b'\n'):
        if line.startswith(b'diff --git '):
            path = None
            continue
        if line.startswith(b'+++ '):
            target = line[4:]
            path = None if target == b'/dev/null' else target[2:]
            continue
        if path is None or not line.startswith(b'+') or excluded(path):
            continue
        added.append(line[1:])

    src = os.path.join(out_dir, 'added-lines.txt')
    with open(src, 'wb') as f:
        f.write(b'\n'.join(added) + b'\n')
    before = sum(1 for line in added if DENY.search(line))
    red = subprocess.run([sys.executable, f'{D}/apply-map.py', '--stdout', f'{D}/redaction-map.txt', src],
                         capture_output=True, check=True).stdout
    after = [line for line in red.split(b'\n') if DENY.search(line)]
    print(f'lines checked {len(added)}, hits before {before}, hits after {len(after)}')
    for line in after[:40]:
        print('  ', line[:200].decode('utf-8', 'replace'))
    return 1 if after else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else tempfile.mkdtemp(prefix='f36-replay-')))
