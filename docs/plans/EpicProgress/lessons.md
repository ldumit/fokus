# Epic Progress — Lessons

## PO Lessons
- When offering clarifying questions and research, bundle all pre-write decisions in the same batch: verification method (cross-check vs critic) and help content (yes/no). Don't ask these after writing — it delays help content and feels like an afterthought.
- External research (competitor analysis) added significant value for this feature — Jira's "skip sprints with ≤2 SP" velocity behavior and Hatica's "Issues w/o Epics" hygiene signal both shaped the spec directly.
- Cross-cutting concerns (C1, C2, C3) must be explicitly addressed even when the feature justifiably omits them. The first critic caught C1/C3 as HIGH because they were silently omitted rather than explicitly justified in Out of Scope.
- When a feature's computation diverges from an existing feature's computation for the same concept (F14 epic completion vs F8 Top Epics), the spec must explicitly scope the alignment work — either in-scope with an AC, or out-of-scope with a backlog item. "Should be aligned" without a clear owner is ambiguous.
- Imputation concepts (inferring missing data from averages) need careful edge case analysis: which tickets contribute to the average, which receive imputed values, and how asymmetry affects user perception. The critic caught that done unestimated tickets vanishing from SP metrics creates unintuitive divergence between ticket % and SP %.
