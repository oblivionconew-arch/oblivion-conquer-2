import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const androidApp = path.join(root, 'android', 'app', 'src', 'main');

if (!fs.existsSync(androidApp)) {
  console.error('ERRO: Diretorio android/ nao encontrado. Execute "npx cap add android" primeiro.');
  process.exit(1);
}

// 1. Copia AndroidManifest.xml (Landscape + Permissoes)
fs.copyFileSync(
  path.join(root, 'android-overrides', 'AndroidManifest.xml'),
  path.join(androidApp, 'AndroidManifest.xml')
);
console.log('[OK] AndroidManifest.xml aplicado (sensorLandscape + permissoes).');

// 2. Copia MainActivity.java (Fullscreen Imersivo + KeepAwake + Autoplay Audio)
const javaPkgDir = path.join(androidApp, 'java', 'br', 'com', 'oblivionco', 'mobile');
fs.mkdirSync(javaPkgDir, { recursive: true });
fs.copyFileSync(
  path.join(root, 'android-overrides', 'MainActivity.java'),
  path.join(javaPkgDir, 'MainActivity.java')
);
console.log('[OK] MainActivity.java aplicado (Fullscreen Imersivo + FLAG_KEEP_SCREEN_ON).');
