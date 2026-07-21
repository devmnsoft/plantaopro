const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const nav = fs.readFileSync(path.join(root,'src/navigation/AppNavigator.tsx'),'utf8');
for (const route of ['home','plantoes','agenda','financeiro','notificacoes','perfil']) { if(!nav.includes(route)) { console.error(`missing navigation route: ${route}`); process.exit(1); } }
console.log('tests ok: core navigation routes are present');
