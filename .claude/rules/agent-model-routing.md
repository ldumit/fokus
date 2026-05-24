# Agent Model Routing

Always set `model: "sonnet"` when spawning general-purpose or Explore agents. They handle web research, file searches, and summarization — tasks that don't need Opus.

```
Agent({
  subagent_type: "general-purpose",
  model: "sonnet",   // REQUIRED
  ...
})

Agent({
  subagent_type: "Explore",
  model: "sonnet",   // REQUIRED
  ...
})
```

## Output Size Constraint

Explore and general-purpose agents return a structured report under 300 words. Write detailed findings to disk (e.g., a notes file or questions.md); the message is a summary. Never return raw file dumps or full grep output as the agent response — summarize findings and cite file paths.
