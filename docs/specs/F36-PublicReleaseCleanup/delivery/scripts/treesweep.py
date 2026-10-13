"""Step 7 tree deny sweep: the plan's regex over the kept tree, then over src/ and client/src/.

usage: python treesweep.py [tree-ish]

Sweeps the working tree, or the given tree-ish (a positive control: pre-public-cleanup must hit).
Prints every hit; exit 0 when both sweeps are empty.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = subprocess.run(['git', 'rev-parse', '--show-toplevel'], cwd=HERE, capture_output=True,
                      check=True, text=True).stdout.strip()
DENY = (r'omnishelf|omniaz|alfamart|\bPD-[0-9]{3,5}\b|project PD\b|shelf.compliance|sprint-rituals|SprintRituals'
        r'|Reflekt|rituals suite|dotnet-microservices|code-forge|C:/Users/Laurentiu|D:[/\\]src'
        r'|\b(Taylor|Mateusz|Adam|Pawel|Paweł|Ovidiu|Norbert|Calin|Maciej)\b')
SWEEPS = {
    'tree': ['.', ':!docs/specs/F36-PublicReleaseCleanup', ':!client/package-lock.json', ':!src', ':!client/src'],
    'src': ['src', 'client/src'],
}


def main(tree):
    clean = True
    for name, pathspec in SWEEPS.items():
        r = subprocess.run(['git', 'grep', '-n', '-i', '-E', DENY, *tree, '--', *pathspec], cwd=REPO,
                           capture_output=True, text=True, encoding='utf-8')
        if r.returncode > 1:
            sys.stderr.write(r.stderr)
            return 2
        hits = r.stdout.splitlines()
        clean &= not hits
        print(f'{name} sweep: {len(hits)} hits in {len({h.split(":")[len(tree)] for h in hits})} files')
        for h in hits[:40]:
            print('  ', h[:200])
    return 0 if clean else 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:2]))
