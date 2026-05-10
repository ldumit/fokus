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
