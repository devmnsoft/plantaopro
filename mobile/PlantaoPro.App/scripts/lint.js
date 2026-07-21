const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(d=>{const p=path.join(dir,d.name);return d.isDirectory()?walk(p):[p];});}
const files = walk(path.join(root,'src')).filter(f=>/\.(ts|tsx|js|jsx)$/.test(f));
const violations=[];
for(const f of files){const c=fs.readFileSync(f,'utf8'); if(/console\.log\s*\([^)]*(token|senha|password|authorization)/i.test(c)) violations.push(`${f}: sensitive token logging`); if(/https:\/\/[^'"`]*(plantaopro|production|prod)/i.test(c)) violations.push(`${f}: fixed production URL`);}
if(!files.length) violations.push('no mobile source files found');
if(violations.length){console.error(violations.join('\n')); process.exit(1);} console.log(`lint ok: ${files.length} source files checked`);
