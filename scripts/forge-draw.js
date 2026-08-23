const fs = require('fs');
const path = require('path');
const roll = JSON.parse(fs.readFileSync('project.json', 'utf-8'));
for (const f of roll.files) {
  fs.mkdirSync(path.dirname(f.path), { recursive: true });
  fs.writeFileSync(f.path, f.body, 'utf-8');
}
const stone = 'ProjectSettings/ProjectVersion.txt';
if (!fs.existsSync(stone)) {
  fs.mkdirSync(path.dirname(stone), { recursive: true });
  fs.writeFileSync(stone, 'm_EditorVersion: 6000.0.32f1\n', 'utf-8');
}
console.log(roll.files.length, 'files laid down');
