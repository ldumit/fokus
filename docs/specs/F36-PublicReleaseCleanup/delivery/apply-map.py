#!/usr/bin/env python3
"""Apply a git filter-repo --replace-text map to working-tree files.

Usage:
  apply-map.py [--stdout] [--from-ref REF] MAP PATH...

Rules are read the way git filter-repo reads them, but only `regex:X==>Y`
rules are accepted: filter-repo runs every literal rule before every regex
rule, so an all-regex map is the only form whose order (file order) is the
same here and in filter-repo. Each rule is applied with re.sub on bytes.

  --from-ref REF  read each file's original bytes from `git show REF:PATH`;
                  where the working file uses CRLF, the LF result is written
                  with CRLF (core.autocrlf=true checks blobs out that way)
  (no ref)        apply the map to the working file's current bytes
  --stdout        print the result for one file instead of writing it
"""
import argparse
import os
import re
import subprocess
import sys


def read_rules(map_path):
    rules = []
    with open(map_path, 'rb') as f:
        for lineno, line in enumerate(f, 1):
            line = line.rstrip(b'\r\n')
            if not line:
                continue
            if b'==>' not in line or not line.startswith(b'regex:'):
                sys.exit(f'{map_path}:{lineno}: only regex:X==>Y rules are allowed')
            pattern, replacement = line.rsplit(b'==>', 1)
            rules.append((re.compile(pattern[len(b'regex:'):]), replacement))
    return rules


def apply_rules(rules, data):
    for regex, replacement in rules:
        data = regex.sub(replacement, data)
    return data


def repo_root():
    out = subprocess.run(['git', 'rev-parse', '--show-toplevel'],
                         capture_output=True, check=True)
    return out.stdout.decode('utf-8').strip()


def to_crlf(data):
    return data.replace(b'\r\n', b'\n').replace(b'\n', b'\r\n')


def is_binary(data):
    return b'\0' in data[:8192]


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--stdout', action='store_true')
    parser.add_argument('--from-ref')
    parser.add_argument('map')
    parser.add_argument('paths', nargs='+')
    args = parser.parse_args()

    if args.stdout and len(args.paths) != 1:
        sys.exit('--stdout takes exactly one path')

    rules = read_rules(args.map)
    root = repo_root() if args.from_ref else None
    changed = skipped = failed = 0

    for path in args.paths:
        working = None
        if os.path.isfile(path):
            with open(path, 'rb') as f:
                working = f.read()

        if args.from_ref:
            if working is None and not args.stdout:
                print(f'error: {path} is not a file in the working tree', file=sys.stderr)
                failed += 1
                continue
            rel = os.path.relpath(os.path.abspath(path), root).replace(os.sep, '/')
            shown = subprocess.run(['git', 'show', f'{args.from_ref}:{rel}'],
                                   capture_output=True, cwd=root)
            if shown.returncode != 0:
                print(f'error: cannot read {args.from_ref}:{rel}', file=sys.stderr)
                failed += 1
                continue
            source = shown.stdout
        else:
            if working is None:
                print(f'error: {path} not found', file=sys.stderr)
                failed += 1
                continue
            source = working

        if is_binary(source):
            print(f'skipped (binary): {path}', file=sys.stderr)
            skipped += 1
            if args.stdout:
                sys.stdout.buffer.write(source)
            continue

        result = apply_rules(rules, source)
        if args.from_ref and working is not None and b'\r\n' in working:
            result = to_crlf(result)

        if args.stdout:
            sys.stdout.buffer.write(result)
        elif result != working:
            with open(path, 'wb') as f:
                f.write(result)
            changed += 1

    if not args.stdout:
        print(f'files: {len(args.paths)}, written: {changed}, '
              f'binary skipped: {skipped}, errors: {failed}', file=sys.stderr)
    sys.exit(1 if failed else 0)


if __name__ == '__main__':
    main()
