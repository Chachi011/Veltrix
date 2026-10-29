// Checks the Georgian translation file against the code: npm run i18n:check
//
// Finds every English text the app translates:
//   'Place bet' | t          in templates (also inline templates in .ts files)
//   .t('Place bet')          in TypeScript (I18n.t)
//   mark('Home')             texts in constants, translated where they are shown
//   title: 'Wallet · VELTRIX' route titles (TranslatedTitleStrategy)
// and compares them with the "ui" section of src/i18n/ka.json.
// Fails (exit code 1) when a text has no Georgian version, or when ka.json has a text the app no longer uses.
// `--missing` prints the missing texts as JSON, ready to be translated.

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const appDir = join(root, 'src', 'app');
const kaFile = join(root, 'src', 'i18n', 'ka.json');

/** Not scanned: development-only pages, and the i18n code itself (its comments show example texts). */
const SKIP = [join('features', 'styleguide'), join('core', 'i18n')];

function files(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) {
      return SKIP.some((skip) => path.endsWith(skip)) ? [] : files(path);
    }
    return /\.(ts|html)$/.test(name) && !name.endsWith('.spec.ts') ? [path] : [];
  });
}

const unescape = (text) => text.replace(/\\\\/g, '\\').replace(/\\(['"`])/g, '$1');

const PATTERNS = [
  // 'text' | t   or   "text" | t   (not | timeAgo etc.)
  /(['"])((?:\\.|(?!\1)[^\\])*?)\1\s*\|\s*t\b(?![\w-])/g,
  // 'Cashed out at {value}' | tSplit
  /(['"])((?:\\.|(?!\1)[^\\])*?)\1\s*\|\s*tSplit\b/g,
  // i18n.t('text'   this.i18n.t("text"
  /\.t\(\s*(['"])((?:\\.|(?!\1)[^\\])*?)\1/g,
  // mark('text')
  /\bmark\(\s*(['"])((?:\\.|(?!\1)[^\\])*?)\1\s*\)/g,
  // title: 'Wallet · VELTRIX'
  /\btitle:\s*(['"])([^'"]+?) · VELTRIX\1/g
];

const used = new Map(); // text → first file that uses it
for (const file of files(appDir)) {
  const source = readFileSync(file, 'utf8');
  for (const pattern of PATTERNS) {
    for (const match of source.matchAll(pattern)) {
      const text = unescape(match[2]);
      if (text && !used.has(text)) {
        used.set(text, relative(root, file));
      }
    }
  }
}

const ka = JSON.parse(readFileSync(kaFile, 'utf8'));
const translated = new Set(Object.keys(ka.ui ?? {}));

const missing = [...used.keys()].filter((text) => !translated.has(text));
const unused = [...translated].filter((text) => !used.has(text));

if (process.argv.includes('--missing')) {
  console.log(JSON.stringify(Object.fromEntries(missing.map((text) => [text, ''])), null, 2));
  process.exit(0);
}

for (const text of missing) {
  console.log(`MISSING  "${text}"  (${used.get(text)})`);
}
for (const text of unused) {
  console.log(`UNUSED   "${text}"`);
}
console.log(`${used.size} texts in the app, ${translated.size} in ka.json: ${missing.length} missing, ${unused.length} unused.`);
process.exit(missing.length || unused.length ? 1 : 0);
