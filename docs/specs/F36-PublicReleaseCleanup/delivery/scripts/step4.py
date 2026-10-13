"""Step 4 driver: apply redaction-map.txt to the working tree, or check it.

usage: python step4.py apply [path ...] | replay | idem

apply   regenerate every candidate file from pre-public-cleanup through the map (writes only on change);
        with paths, only those paths are considered
replay  for every candidate changed since pre-public-cleanup, map output from the ref equals the working
        file after CRLF->LF on both sides
idem    a second in-place pass over every changed file changes nothing

Candidates are text files outside PROTECTED, HAND_EDITED, LEFT_OUT and exclusions.txt. HAND_EDITED files
carry edits no map makes, so they take the in-place mode (idem) and never the from-ref regeneration.
apply and replay skip, with a note, any candidate the reference ref does not hold.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = subprocess.run(['git', 'rev-parse', '--show-toplevel'], cwd=HERE, capture_output=True,
                      check=True, text=True).stdout.strip()
D = 'docs/specs/F36-PublicReleaseCleanup/delivery'
REF = 'pre-public-cleanup'
PROTECTED = ('src/', 'client/', 'docs/specs/F36-PublicReleaseCleanup/')
HAND_EDITED = {'.gitignore', '.claude/settings.json', 'README.md', 'deploy.ps1',
               'overnight-pipeline.ps1', 'docs/backlog.md', 'docs/product/v1.md',
               'docs/specs/GAP-1-excluded-statuses-ui/definition/spec.md',
               'docs/specs/GAP-2-sprint-range-picker/definition/spec.md',
               'docs/specs/GAP-3-sub-team-management-ui/definition/spec.md'}
LEFT_OUT = {'.claude/settings.local.json'}


def git_paths(*args):
    out = subprocess.run(['git', *args, '-z'], capture_output=True, check=True).stdout.split(b'\0')
    return [p.decode('utf-8') for p in out if p]


def load_exclusions():
    with open(f'{D}/exclusions.txt', encoding='utf-8') as f:
        return [line.strip() for line in f if line.strip()]


def excluded(path, excl):
    # filter-repo's prefix rule, as replay.py applies it
    return any(path.startswith(e) and (e.endswith('/') or len(path) == len(e) or path[len(e):len(e) + 1] == '/')
               for e in excl)


def note(label, paths):
    if paths:
        print(f'skipped ({label}):', len(paths))
        for p in paths:
            print('  ', p)


def candidates(paths, need_ref):
    excl = load_exclusions()
    in_ref = set(git_paths('ls-tree', '-r', '--name-only', REF)) if need_ref else None
    out, skipped_excl, skipped_ref = [], [], []
    for p in paths:
        if p.startswith(PROTECTED) or p in HAND_EDITED or p in LEFT_OUT or not os.path.isfile(p):
            continue
        if excluded(p, excl):
            skipped_excl.append(p)
            continue
        with open(p, 'rb') as f:
            if b'\0' in f.read(8192):
                continue
        if need_ref and p not in in_ref:
            skipped_ref.append(p)
            continue
        out.append(p)
    note('in exclusions.txt', skipped_excl)
    note(f'not in {REF}', skipped_ref)
    sys.stdout.flush()
    return out


def run(args):
    return subprocess.run([sys.executable, f'{D}/apply-map.py'] + args, capture_output=True)


def main(mode, paths):
    os.chdir(REPO)
    if mode == 'apply':
        files = candidates(paths or git_paths('ls-files'), need_ref=True)
        for i in range(0, len(files), 150):  # stay under the Windows command-line limit
            r = run(['--from-ref', REF, f'{D}/redaction-map.txt'] + files[i:i + 150])
            sys.stdout.buffer.write(r.stdout)
            sys.stderr.buffer.write(r.stderr)
            if r.returncode:
                return r.returncode
        print('candidates', len(files))
    elif mode == 'replay':
        files = candidates(git_paths('diff', '--name-only', REF), need_ref=True)
        bad = []
        for p in files:
            r = run(['--stdout', '--from-ref', REF, f'{D}/redaction-map.txt', p])
            with open(p, 'rb') as f:
                w = f.read()
            if r.returncode or r.stdout.replace(b'\r\n', b'\n') != w.replace(b'\r\n', b'\n'):
                bad.append(p)
        print('replay: files checked', len(files), 'mismatches', len(bad))
        for b in bad:
            print('  ', b)
        return 1 if bad else 0
    elif mode == 'idem':
        excl = load_exclusions()
        files = [p for p in git_paths('diff', '--name-only', REF)
                 if os.path.isfile(p) and not p.startswith(PROTECTED) and p not in LEFT_OUT
                 and not excluded(p, excl)]
        bad = []
        for p in files:
            r = run(['--stdout', f'{D}/redaction-map.txt', p])
            with open(p, 'rb') as f:
                if r.returncode or r.stdout != f.read():
                    bad.append(p)
        print('idempotence: files checked', len(files), 'changed by a second pass', len(bad))
        for b in bad:
            print('  ', b)
        return 1 if bad else 0
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else '', sys.argv[2:]))
