#!/usr/bin/env node
// SessionStart hook — re-inject the last active pipeline role after clear/compact/resume.
// Reads .claude/.current-agent (written by the /<agent> commands). Graceful no-op if absent.
// OMC-free replacement for the old `echo "be $CC_AGENT"` hook ($CC_AGENT was never set here).

const fs = require("fs");
const path = require("path");

let raw = "";
process.stdin.on("data", (c) => (raw += c));
process.stdin.on("end", () => {
  let source = "";
  try { source = JSON.parse(raw || "{}").source || ""; } catch { /* ignore */ }

  const root = process.env.CLAUDE_PROJECT_DIR || process.cwd();
  const stateFile = path.join(root, ".claude", ".current-agent");

  let agent = "";
  try { agent = fs.readFileSync(stateFile, "utf8").trim(); } catch { /* none */ }

  // Validate slug — defends against junk/injection in the state file.
  if (!agent || !/^[a-z][a-z-]{1,30}$/.test(agent)) process.exit(0);

  const resuming = source === "clear" || source === "compact" || source === "resume";
  const msg = resuming
    ? `Resuming role: you were acting as the **${agent}**. Re-read \`.claude/agents/${agent}.md\` and continue in that role.`
    : `Last active role in this project was **${agent}**. Run \`/${agent}\` to resume it, or proceed normally.`;

  process.stdout.write(msg + "\n");
  process.exit(0);
});
