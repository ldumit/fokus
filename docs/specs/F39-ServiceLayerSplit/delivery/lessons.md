# F39 — Service Layer Split — Lessons

## Architect Lessons

- **A refactor proven by output comparison needs its harness specified like production code.** Round 1 found 6 HIGH gaps in a one-paragraph harness:
  - error bodies carrying trace ids and stack traces;
  - the signed-in user's database row;
  - start-up seeding;
  - feature flags off by default;
  - routes outside the request list;
  - filters never sent.

  Spell out the host, the identity, the fixture variants, the request manifest with expected statuses, and a sabotage check before calling it a pin.
- **Check every reply field for machine-specific text before promising byte equality.** A sync service put `ex.StackTrace` straight into its reply.
- **Move tests in the same step as the type they test** whenever each step must stay green. A trailing "fix tests" step leaves the build red for every step before it.
- **Count before scoping.** "Three long methods" was 25 methods over the plan's own limit; a method-length script at plan time would have sized the slice correctly.
- **Accept commands are written for the shell that runs them.** Under Git Bash, an unquoted Windows path loses its backslashes, and `find -name "{a,b}"` matches the braces literally.
