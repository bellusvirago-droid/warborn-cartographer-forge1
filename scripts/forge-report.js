const fs = require('fs');
const path = require('path');

let log = '';
const candidates = ['Logs/Build.log', 'build.log'];
function walk(dir) {
  let results = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) results = results.concat(walk(full));
    else if (entry.name.endsWith('.log')) results.push(full);
  }
  return results;
}
try { candidates.push(...walk('.').sort()); } catch (e) {}
for (const name of candidates) {
  if (fs.existsSync(name)) {
    const text = fs.readFileSync(name, 'utf-8');
    if (text.trim()) { log = text; break; }
  }
}
if (!log) log = 'No build log was written. The runner never reached the compiler.';

const faults = [];
const re = /^(.*?\.cs)\((\d+),\d+\): error (CS\d+): (.*)$/gm;
let m;
while ((m = re.exec(log)) !== null) {
  faults.push({
    path: m[1].split('TheMarch/').pop(),
    line: parseInt(m[2], 10),
    rule: m[3],
    word: m[4].trim(),
  });
}

const passed = process.env.OUTCOME === 'success' && faults.length === 0;
const body = JSON.stringify({
  nonce: process.env.NONCE,
  passed: passed,
  runUrl: process.env.RUN_URL,
  log: log.slice(-30000),
  faults: faults.slice(0, 200),
});

(async () => {
  for (let attempt = 0; attempt < 4; attempt++) {
    try {
      const res = await fetch(process.env.BASE + '/api/public/forge/report', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'Warborn-Forge-Runner/1.0',
        },
        body: body,
      });
      console.log(await res.text());
      return;
    } catch (err) {
      console.log('report attempt', attempt + 1, 'failed:', err.message);
      await new Promise(r => setTimeout(r, 5000 * (attempt + 1)));
    }
  }
  console.log('The house never took the report.');
})();
