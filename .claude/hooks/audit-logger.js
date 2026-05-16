let data = '';
process.stdin.on('data', chunk => data += chunk);
process.stdin.on('end', () => {
  const fs = require('fs');
  const path = require('path');

  let input;
  try { input = JSON.parse(data); } catch { process.exit(0); }

  const sessionId = input.session_id || 'unknown';
  const event = input.hook_event_name || 'unknown';
  const tool = input.tool_name || 'none';
  const agentType = input.agent_type || 'main';
  const timestamp = new Date().toISOString();

  let detail = '';
  if (tool === 'Skill' && input.tool_input) {
    detail = `skill=${input.tool_input.skill || 'unknown'}`;
  } else if (input.tool_input) {
    const keys = Object.keys(input.tool_input).filter(k => k !== 'content' && k !== 'new_string' && k !== 'old_string');
    detail = keys.map(k => {
      const v = input.tool_input[k];
      const s = typeof v === 'string' ? v.substring(0, 120) : String(v).substring(0, 120);
      return `${k}=${s}`;
    }).join(' | ');
  }

  const auditDir = path.join(input.cwd || process.cwd(), 'docs', 'audit');
  fs.mkdirSync(auditDir, { recursive: true });

  const logFile = path.join(auditDir, `${sessionId}.log`);
  const line = `${timestamp} | ${event} | tool=${tool} | agent=${agentType} | ${detail}\n`;
  fs.appendFileSync(logFile, line);
});
