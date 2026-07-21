const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const api = fs.readFileSync(path.join(root,'src/services/api.ts'),'utf8');
if(!api.includes('EXPO_PUBLIC_API_BASE_URL')) { console.error('EXPO_PUBLIC_API_BASE_URL is required in mobile API service'); process.exit(1); }
console.log('typecheck ok: API base URL uses EXPO_PUBLIC_API_BASE_URL');
