#!/usr/bin/env node
const fs = require('fs');
const fsp = fs.promises;
const path = require('path');
const { URL } = require('url');
const crypto = require('crypto');

if (typeof fetch !== 'function') {
  console.error('Diese Node.js-Version stellt kein globales fetch zur Verfügung. Bitte Node 18 oder neuer verwenden.');
  process.exit(1);
}

const inputUrl = process.argv[2] || 'https://hps-power.com';
let baseUrl;
try {
  baseUrl = new URL(inputUrl);
} catch (error) {
  console.error(`Ungültige Basis-URL: ${inputUrl}`);
  process.exit(1);
}

if (!['http:', 'https:'].includes(baseUrl.protocol)) {
  console.error('Nur http und https werden unterstützt.');
  process.exit(1);
}

const OUTPUT_ROOT = path.resolve(__dirname, '../public/legacy');
const envDepth = Number(process.env.LEGACY_MAX_DEPTH);
const MAX_DEPTH = Number.isFinite(envDepth) && envDepth >= 0 ? envDepth : 2;
const SHOULD_CLEAN = process.env.LEGACY_SKIP_CLEAN === 'true' ? false : true;

const visitedPages = new Set();
const pageQueue = [];
const assetQueue = new Map();

pageQueue.push({ url: normalizedPageUrl(baseUrl), depth: 0 });

async function ensureDir(dirPath) {
  await fsp.mkdir(dirPath, { recursive: true });
}

function normalizedPageUrl(url) {
  const target = url instanceof URL ? url : new URL(url, baseUrl);
  if (!target.pathname || target.pathname === '') {
    target.pathname = '/';
  }
  if (target.hash) {
    target.hash = '';
  }
  return target.href;
}

function sanitizeFileName(filePath) {
  return filePath.replace(/\\+/g, '/');
}

function pageOutputPath(urlStr) {
  const urlObj = new URL(urlStr);
  let pathname = urlObj.pathname || '/';
  if (pathname.endsWith('/')) {
    pathname = `${pathname}index.html`;
  } else if (!path.extname(pathname)) {
    pathname = `${pathname}.html`;
  }
  pathname = sanitizeFileName(pathname.replace(/^\//, ''));
  if (urlObj.search) {
    pathname = appendQueryDigest(pathname, urlObj.search);
  }
  return path.join(OUTPUT_ROOT, pathname);
}

function assetOutputPath(urlStr) {
  const urlObj = new URL(urlStr);
  let pathname = urlObj.pathname || '/asset.bin';
  if (pathname.endsWith('/')) {
    pathname = `${pathname}index`;
  }
  pathname = sanitizeFileName(pathname.replace(/^\//, ''));
  if (!pathname) {
    pathname = 'asset.bin';
  }
  if (urlObj.search) {
    pathname = appendQueryDigest(pathname, urlObj.search);
  }
  return path.join(OUTPUT_ROOT, pathname);
}

function appendQueryDigest(basePath, query) {
  if (!query) {
    return basePath;
  }
  const digest = crypto.createHash('sha1').update(query).digest('hex').slice(0, 10);
  const ext = path.extname(basePath);
  if (!ext) {
    return `${basePath}-${digest}`;
  }
  return `${basePath.slice(0, -ext.length)}-${digest}${ext}`;
}

function collectLinks(html, currentUrl) {
  const links = [];
  const attrPattern = /\b(?:href|src|data|xlink:href)="([^"]+)"/gi;
  let match;
  while ((match = attrPattern.exec(html))) {
    links.push(match[1]);
  }

  const singleQuotePattern = /\b(?:href|src|data|xlink:href)='([^']+)'/gi;
  while ((match = singleQuotePattern.exec(html))) {
    links.push(match[1]);
  }

  const srcsetPattern = /\bsrcset="([^"]+)"/gi;
  while ((match = srcsetPattern.exec(html))) {
    const entries = match[1].split(',');
    for (const entry of entries) {
      const candidate = entry.trim().split(/\s+/)[0];
      if (candidate) {
        links.push(candidate);
      }
    }
  }

  const srcsetSinglePattern = /\bsrcset='([^']+)'/gi;
  while ((match = srcsetSinglePattern.exec(html))) {
    const entries = match[1].split(',');
    for (const entry of entries) {
      const candidate = entry.trim().split(/\s+/)[0];
      if (candidate) {
        links.push(candidate);
      }
    }
  }

  const results = [];
  for (const raw of links) {
    if (!raw || raw.startsWith('#')) {
      continue;
    }
    const lowered = raw.toLowerCase();
    if (lowered.startsWith('mailto:') || lowered.startsWith('tel:') || lowered.startsWith('javascript:') || lowered.startsWith('data:')) {
      continue;
    }
    try {
      const resolved = new URL(raw, currentUrl);
      if (resolved.origin !== baseUrl.origin) {
        continue;
      }
      results.push(resolved.href);
    } catch (error) {
      // ignore parsing issues
    }
  }
  return results;
}

function isAssetUrl(urlStr) {
  const urlObj = new URL(urlStr);
  const ext = path.extname(urlObj.pathname).toLowerCase();
  if (!ext) {
    return false;
  }
  return [
    '.png',
    '.jpg',
    '.jpeg',
    '.webp',
    '.gif',
    '.svg',
    '.ico',
    '.css',
    '.js',
    '.json',
    '.pdf',
    '.zip',
    '.woff',
    '.woff2',
    '.ttf',
    '.otf'
  ].includes(ext);
}

async function fetchPage(target, depth) {
  const normalized = normalizedPageUrl(target);
  if (visitedPages.has(normalized)) {
    return;
  }
  visitedPages.add(normalized);

  let response;
  try {
    response = await fetch(normalized, {
      headers: {
        'user-agent': 'hps-sync-script/1.0'
      }
    });
  } catch (error) {
    console.error(`Fehler beim Laden von ${normalized}:`, error.message);
    return;
  }

  if (!response.ok) {
    console.error(`Antwort ${response.status} für ${normalized}`);
    return;
  }

  const arrayBuffer = await response.arrayBuffer();
  const buffer = Buffer.from(arrayBuffer);
  const html = buffer.toString('utf8');
  const outputPath = pageOutputPath(normalized);
  await ensureDir(path.dirname(outputPath));
  await fsp.writeFile(outputPath, html, 'utf8');
  console.log(`Seite gespeichert: ${normalized} -> ${path.relative(OUTPUT_ROOT, outputPath)}`);

  const discovered = collectLinks(html, normalized);
  for (const link of discovered) {
    if (isAssetUrl(link)) {
      if (!assetQueue.has(link)) {
        assetQueue.set(link, assetOutputPath(link));
      }
      continue;
    }

    if (depth + 1 <= MAX_DEPTH) {
      pageQueue.push({ url: link, depth: depth + 1 });
    }
  }
}

async function fetchAsset(urlStr, outputPath) {
  let response;
  try {
    response = await fetch(urlStr, {
      headers: {
        'user-agent': 'hps-sync-script/1.0'
      }
    });
  } catch (error) {
    console.error(`Fehler beim Laden der Ressource ${urlStr}:`, error.message);
    return;
  }

  if (!response.ok) {
    console.error(`Antwort ${response.status} für Ressource ${urlStr}`);
    return;
  }

  const arrayBuffer = await response.arrayBuffer();
  const buffer = Buffer.from(arrayBuffer);
  await ensureDir(path.dirname(outputPath));
  await fsp.writeFile(outputPath, buffer);
  console.log(`Asset gespeichert: ${urlStr} -> ${path.relative(OUTPUT_ROOT, outputPath)}`);
}

async function main() {
  if (SHOULD_CLEAN) {
    await fsp.rm(OUTPUT_ROOT, { recursive: true, force: true });
  }
  await ensureDir(OUTPUT_ROOT);

  while (pageQueue.length > 0) {
    const { url, depth } = pageQueue.shift();
    await fetchPage(url, depth);
  }

  for (const [urlStr, outputPath] of assetQueue.entries()) {
    await fetchAsset(urlStr, outputPath);
  }

  const manifest = {
    baseUrl: baseUrl.href,
    generatedAt: new Date().toISOString(),
    pageCount: visitedPages.size,
    assetCount: assetQueue.size,
    maxDepth: MAX_DEPTH
  };

  await fsp.writeFile(path.join(OUTPUT_ROOT, 'legacy-manifest.json'), JSON.stringify(manifest, null, 2), 'utf8');
  console.log('Legacy-Inhalte synchronisiert.');
}

main().catch((error) => {
  console.error('Legacy-Synchronisation fehlgeschlagen:', error);
  process.exit(1);
});
