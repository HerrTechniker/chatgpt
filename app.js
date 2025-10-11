const http = require('http');
const fs = require('fs');
const fsp = fs.promises;
const path = require('path');
const { URL } = require('url');
const crypto = require('crypto');

const HOST = '0.0.0.0';
const PORT = process.env.PORT || 3000;

const DATA_DIR = path.join(__dirname, 'data');
const PUBLIC_DIR = path.join(__dirname, 'public');
const DOWNLOAD_DIR = path.join(PUBLIC_DIR, 'downloads');
const BOOKING_UPLOAD_DIR = path.join(DATA_DIR, 'booking_uploads');
const USERS_FILE = path.join(DATA_DIR, 'users.json');
const DOWNLOADS_FILE = path.join(DATA_DIR, 'downloads.json');
const BOOKINGS_FILE = path.join(DATA_DIR, 'bookings.json');

const COMPANY_NOTIFICATION_EMAIL = process.env.COMPANY_NOTIFICATION_EMAIL || 'info@hps-power.local';
const MAIL_SENDER = process.env.MAIL_SENDER || 'HPS Power Solutions <info@hps-power.local>';

const SESSION_COOKIE = 'sessionId';
const SESSION_DURATION = 1000 * 60 * 60 * 8; // 8 hours
const sessions = new Map();
const MAX_REQUEST_SIZE = 25 * 1024 * 1024; // 25 MB
const MAX_BOOKING_FILE_SIZE = 12 * 1024 * 1024; // 12 MB pro Datei

const pageRoutes = {
  '/': 'index.html',
  '/index.html': 'index.html',
  '/solutions': 'solutions.html',
  '/services': 'services.html',
  '/industries': 'industries.html',
  '/downloads': 'downloads.html',
  '/contact': 'contact.html',
  '/about': 'about.html',
  '/internships': 'internships.html',
  '/internships.html': 'internships.html',
  '/legal': 'legal.html',
  '/legal.html': 'legal.html',
  '/impressum': 'legal.html',
  '/datenschutz': 'legal.html'
};

async function ensureDataFiles() {
  await fsp.mkdir(DATA_DIR, { recursive: true });
  await fsp.mkdir(DOWNLOAD_DIR, { recursive: true });
  await fsp.mkdir(BOOKING_UPLOAD_DIR, { recursive: true });

  try {
    await fsp.access(USERS_FILE);
  } catch {
    await fsp.writeFile(USERS_FILE, '[]', 'utf8');
  }

  try {
    await fsp.access(DOWNLOADS_FILE);
  } catch {
    await fsp.writeFile(DOWNLOADS_FILE, '[]', 'utf8');
  }

  try {
    await fsp.access(BOOKINGS_FILE);
  } catch {
    await fsp.writeFile(BOOKINGS_FILE, '[]', 'utf8');
  }

  const users = await readUsers();
  if (!users.some((user) => user.role === 'admin')) {
    const adminUser = {
      id: generateId(),
      name: 'System Administrator',
      email: 'admin@hps-power.local',
      role: 'admin',
      password: hashPassword('ChangeMe123!')
    };
    users.push(adminUser);
    await writeUsers(users);
    console.log('\nDefault admin account created:');
    console.log('Email: admin@hps-power.local');
    console.log('Password: ChangeMe123!\n');
  }
}

function generateId() {
  return crypto.randomUUID ? crypto.randomUUID() : crypto.randomBytes(16).toString('hex');
}

function hashPassword(password, salt = crypto.randomBytes(16).toString('hex')) {
  const derivedKey = crypto.scryptSync(password, salt, 64).toString('hex');
  return `${salt}:${derivedKey}`;
}

function verifyPassword(password, stored) {
  if (!stored) {
    return false;
  }
  const [salt, key] = stored.split(':');
  if (!salt || !key) {
    return false;
  }
  const derivedKey = crypto.scryptSync(password, salt, 64).toString('hex');
  const keyBuffer = Buffer.from(key, 'hex');
  const derivedBuffer = Buffer.from(derivedKey, 'hex');
  if (keyBuffer.length !== derivedBuffer.length) {
    return false;
  }
  return crypto.timingSafeEqual(keyBuffer, derivedBuffer);
}

async function readUsers() {
  const content = await fsp.readFile(USERS_FILE, 'utf8');
  return JSON.parse(content);
}

async function writeUsers(users) {
  await fsp.writeFile(USERS_FILE, JSON.stringify(users, null, 2), 'utf8');
}

async function readDownloads() {
  const content = await fsp.readFile(DOWNLOADS_FILE, 'utf8');
  return JSON.parse(content);
}

async function writeDownloads(downloads) {
  await fsp.writeFile(DOWNLOADS_FILE, JSON.stringify(downloads, null, 2), 'utf8');
}

async function readBookings() {
  const content = await fsp.readFile(BOOKINGS_FILE, 'utf8');
  return JSON.parse(content);
}

async function writeBookings(bookings) {
  await fsp.writeFile(BOOKINGS_FILE, JSON.stringify(bookings, null, 2), 'utf8');
}

let nodemailerModulePromise = null;
let mailTransport = null;
let mailTransportInitialized = false;

async function loadNodemailer() {
  if (!nodemailerModulePromise) {
    nodemailerModulePromise = import('nodemailer')
      .then((module) => module.default || module)
      .catch((error) => {
        console.warn('Nodemailer konnte nicht geladen werden:', error.message);
        return null;
      });
  }
  return nodemailerModulePromise;
}

async function getMailTransport() {
  if (mailTransportInitialized) {
    return mailTransport;
  }
  const host = process.env.SMTP_HOST;
  if (!host) {
    mailTransportInitialized = true;
    mailTransport = null;
    return null;
  }
  const nodemailer = await loadNodemailer();
  if (!nodemailer) {
    mailTransportInitialized = true;
    mailTransport = null;
    return null;
  }
  const port = Number(process.env.SMTP_PORT || 587);
  const secure = String(process.env.SMTP_SECURE || '').toLowerCase() === 'true';
  const user = process.env.SMTP_USER;
  const pass = process.env.SMTP_PASS;
  const transportOptions = { host, port, secure };
  if (user) {
    transportOptions.auth = { user, pass: pass || '' };
  }
  mailTransport = nodemailer.createTransport(transportOptions);
  mailTransportInitialized = true;
  return mailTransport;
}

async function sendMail(message) {
  const transport = await getMailTransport();
  if (!transport) {
    console.log('E-Mail-Versand nicht konfiguriert. Nachricht wäre gesendet worden:', message);
    return;
  }
  try {
    await transport.sendMail({ from: MAIL_SENDER, ...message });
  } catch (error) {
    console.error('E-Mail-Versand fehlgeschlagen', error);
  }
}

function formatDateTime(dateString) {
  const formatter = new Intl.DateTimeFormat('de-DE', { dateStyle: 'long', timeStyle: 'short' });
  return formatter.format(new Date(dateString));
}

async function notifyBooking(booking) {
  const period = `${formatDateTime(booking.startDateTime)} – ${formatDateTime(booking.endDateTime)}`;
  const applicantMessage = {
    to: booking.email,
    subject: 'Eingangsbestätigung Praktikumsanfrage',
    text: `Hallo ${booking.name},\n\nwir haben Ihre Anfrage für ein Praktikum im Zeitraum ${period} erhalten. Unser Team meldet sich zeitnah bei Ihnen.\n\nVielen Dank und herzliche Grüße\nHPS Power Solutions`
  };

  const companyMessage = {
    to: COMPANY_NOTIFICATION_EMAIL,
    subject: `Neue Praktikumsanfrage: ${booking.name}`,
    text: `Es wurde eine neue Praktikumsanfrage eingereicht.\n\nName: ${booking.name}\nE-Mail: ${booking.email}\nTelefon: ${booking.phone || 'nicht angegeben'}\nZeitraum: ${period}\nStatus: ${booking.status}\nNachricht: ${booking.note || '—'}\n\nErstellt am: ${formatDateTime(booking.createdAt)}\n`
  };

  try {
    if (booking.email) {
      await sendMail(applicantMessage);
    }
    await sendMail(companyMessage);
  } catch (error) {
    console.error('Benachrichtigung der Buchung fehlgeschlagen', error);
  }
}

function sanitizePublicBooking(booking) {
  return {
    id: booking.id,
    startDateTime: booking.startDateTime,
    endDateTime: booking.endDateTime,
    status: booking.status || 'pending'
  };
}

function mapAdminBooking(booking) {
  const attachments = Array.isArray(booking.attachments)
    ? booking.attachments.map((attachment) => ({
        ...attachment,
        downloadUrl: `/api/bookings/${booking.id}/files/${attachment.id}`
      }))
    : [];
  return { ...booking, attachments };
}

function createSession(userId) {
  const sessionId = generateId();
  const expires = Date.now() + SESSION_DURATION;
  sessions.set(sessionId, { userId, expires });
  return sessionId;
}

function getSession(req) {
  const cookies = parseCookies(req.headers.cookie);
  const sessionId = cookies[SESSION_COOKIE];
  if (!sessionId) {
    return null;
  }
  const session = sessions.get(sessionId);
  if (!session) {
    return null;
  }
  if (session.expires < Date.now()) {
    sessions.delete(sessionId);
    return null;
  }
  session.expires = Date.now() + SESSION_DURATION;
  return { ...session, sessionId };
}

function setSessionCookie(res, sessionId) {
  res.setHeader('Set-Cookie', `${SESSION_COOKIE}=${encodeURIComponent(sessionId)}; HttpOnly; Path=/; Max-Age=${SESSION_DURATION / 1000}`);
}

function clearSessionCookie(res) {
  res.setHeader('Set-Cookie', `${SESSION_COOKIE}=; HttpOnly; Path=/; Max-Age=0`);
}

async function readRequestBuffer(req, limit = MAX_REQUEST_SIZE) {
  const chunks = [];
  let total = 0;
  for await (const chunk of req) {
    total += chunk.length;
    if (total > limit) {
      const error = new Error('Request entity too large');
      error.code = 'LIMIT_EXCEEDED';
      throw error;
    }
    chunks.push(chunk);
  }
  return Buffer.concat(chunks);
}

function parseCookies(cookieHeader) {
  const cookies = {};
  if (!cookieHeader) {
    return cookies;
  }
  const pairs = cookieHeader.split(';');
  for (const pair of pairs) {
    const [name, value] = pair.trim().split('=');
    if (name && value) {
      cookies[name] = decodeURIComponent(value);
    }
  }
  return cookies;
}

function sanitizeEmailInput(value) {
  if (typeof value !== 'string') {
    return '';
  }
  return value.replace(/[\u0000\r\n]/g, '').trim();
}

function sanitizePasswordInput(value) {
  if (typeof value !== 'string') {
    return '';
  }
  return value.replace(/[\u0000\r\n]/g, '');
}

function isValidEmailAddress(email) {
  if (!email || email.length > 254) {
    return false;
  }
  const emailPattern = /^[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-z0-9-]+(?:\.[a-z0-9-]+)+$/i;
  return emailPattern.test(email);
}

const SUSPICIOUS_LOGIN_PATTERNS = [
  /('|")\s*or\s+1=1/i,
  /('|")\s*or\s+('|\")[^'\"]+('|\")\s*=\s*('|\")[^'\"]+('|\")/i,
  /;\s*(?:drop|delete|insert|update|exec|create)\b/i,
  /\bunion\s+select\b/i,
  /\bwaitfor\s+delay\b/i,
  /\bsleep\s*\(/i,
  /\bbenchmark\s*\(/i,
  /(?:^|[\s'\"])--/i,
  /\/\*/,
  /\bxp_/i
];

function isSuspiciousLoginValue(value) {
  if (typeof value !== 'string' || !value) {
    return false;
  }
  return SUSPICIOUS_LOGIN_PATTERNS.some((pattern) => pattern.test(value));
}

function mergeFieldValue(container, name, value) {
  if (container[name] === undefined) {
    container[name] = value;
    return;
  }
  if (Array.isArray(container[name])) {
    container[name].push(value);
    return;
  }
  container[name] = [container[name], value];
}

function getFieldValue(fields, key) {
  const value = fields[key];
  if (Array.isArray(value)) {
    const last = value[value.length - 1];
    return last === undefined ? '' : String(last).trim();
  }
  if (value === undefined || value === null) {
    return '';
  }
  return String(value).trim();
}

function parseContentDisposition(header) {
  const parts = header.split(';').map((part) => part.trim());
  const type = parts.shift();
  const params = {};
  for (const part of parts) {
    const [key, ...rest] = part.split('=');
    if (!key) continue;
    let value = rest.join('=').trim();
    if (value.startsWith('"') && value.endsWith('"')) {
      value = value.slice(1, -1);
    }
    params[key.toLowerCase()] = value;
  }
  return { type: type ? type.toLowerCase() : '', params };
}

function parseMultipartBody(buffer, boundary) {
  const result = { fields: {}, files: [] };
  const boundaryBuffer = Buffer.from(`--${boundary}`);
  let position = 0;

  while (position < buffer.length) {
    const boundaryIndex = buffer.indexOf(boundaryBuffer, position);
    if (boundaryIndex === -1) {
      break;
    }
    position = boundaryIndex + boundaryBuffer.length;
    if (buffer[position] === 45 && buffer[position + 1] === 45) {
      break;
    }
    if (buffer[position] === 13 && buffer[position + 1] === 10) {
      position += 2;
    }
    const nextBoundaryIndex = buffer.indexOf(boundaryBuffer, position);
    if (nextBoundaryIndex === -1) {
      break;
    }
    let part = buffer.slice(position, nextBoundaryIndex);
    if (part.length >= 2 && part[part.length - 2] === 13 && part[part.length - 1] === 10) {
      part = part.slice(0, -2);
    }
    const headerEndIndex = part.indexOf(Buffer.from('\r\n\r\n'));
    if (headerEndIndex === -1) {
      position = nextBoundaryIndex;
      continue;
    }
    const headerText = part.slice(0, headerEndIndex).toString('utf8');
    const body = part.slice(headerEndIndex + 4);
    const headers = {};
    for (const line of headerText.split('\r\n')) {
      const [name, ...rest] = line.split(':');
      if (!name) continue;
      headers[name.trim().toLowerCase()] = rest.join(':').trim();
    }
    const disposition = headers['content-disposition'];
    if (!disposition) {
      position = nextBoundaryIndex;
      continue;
    }
    const { params } = parseContentDisposition(disposition);
    const fieldName = params.name;
    if (!fieldName) {
      position = nextBoundaryIndex;
      continue;
    }
    if (params.filename) {
      result.files.push({
        fieldName,
        originalName: params.filename,
        contentType: headers['content-type'] || 'application/octet-stream',
        buffer: body,
        size: body.length
      });
    } else {
      mergeFieldValue(result.fields, fieldName, body.toString('utf8'));
    }
    position = nextBoundaryIndex;
  }

  return result;
}

async function parseMultipartForm(req) {
  const contentType = req.headers['content-type'] || '';
  const boundaryMatch = contentType.match(/boundary=(?:"([^"]+)"|([^;]+))/i);
  if (!boundaryMatch) {
    const error = new Error('Multipart boundary missing');
    error.code = 'BAD_MULTIPART';
    throw error;
  }
  const boundary = boundaryMatch[1] || boundaryMatch[2];
  const buffer = await readRequestBuffer(req, MAX_REQUEST_SIZE);
  return parseMultipartBody(buffer, boundary);
}

async function parseBody(req) {
  const buffer = await readRequestBuffer(req, MAX_REQUEST_SIZE);
  const raw = buffer.toString();
  const contentType = req.headers['content-type'] || '';
  if (contentType.includes('application/json')) {
    if (!raw) {
      return {};
    }
    try {
      return JSON.parse(raw);
    } catch (error) {
      throw new Error('Invalid JSON body');
    }
  }
  if (contentType.includes('application/x-www-form-urlencoded')) {
    const params = new URLSearchParams(raw);
    return Object.fromEntries(params.entries());
  }
  return raw;
}

function respondJson(res, statusCode, data) {
  res.writeHead(statusCode, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(data));
}

function sanitizeFileName(fileName) {
  return fileName.replace(/[^a-zA-Z0-9.\-_]/g, '-');
}

async function saveBookingAttachments(bookingId, files) {
  if (!files || !files.length) {
    return [];
  }
  const targetDir = path.join(BOOKING_UPLOAD_DIR, bookingId);
  await fsp.mkdir(targetDir, { recursive: true });
  const saved = [];
  for (const file of files) {
    if (!file || !file.originalName || !file.buffer || !file.size) {
      continue;
    }
    const ext = path.extname(file.originalName);
    const base = path.basename(file.originalName, ext);
    const safeBase = sanitizeFileName(base || 'datei');
    const safeExt = sanitizeFileName(ext || '');
    const uniqueId = generateId();
    const storedName = sanitizeFileName(`${uniqueId}-${safeBase}${safeExt}`);
    const relativePath = path.join(bookingId, storedName);
    const absolutePath = path.join(BOOKING_UPLOAD_DIR, relativePath);
    await fsp.writeFile(absolutePath, file.buffer);
    saved.push({
      id: uniqueId,
      field: file.fieldName,
      originalName: file.originalName,
      mimeType: file.contentType,
      size: file.size,
      path: relativePath,
      uploadedAt: new Date().toISOString()
    });
  }
  return saved;
}

async function removeBookingAttachments(booking) {
  if (!booking || !Array.isArray(booking.attachments) || !booking.attachments.length) {
    return;
  }
  for (const attachment of booking.attachments) {
    if (!attachment.path) continue;
    const filePath = path.join(BOOKING_UPLOAD_DIR, attachment.path);
    if (!filePath.startsWith(BOOKING_UPLOAD_DIR)) {
      continue;
    }
    try {
      await fsp.unlink(filePath);
    } catch (error) {
      if (error.code !== 'ENOENT') {
        console.warn('Konnte Anhang nicht löschen:', filePath, error.message);
      }
    }
  }
  const bookingDir = path.join(BOOKING_UPLOAD_DIR, booking.id);
  try {
    await fsp.rm(bookingDir, { recursive: true, force: true });
  } catch (error) {
    if (error.code !== 'ENOENT') {
      console.warn('Konnte Upload-Verzeichnis nicht löschen:', bookingDir, error.message);
    }
  }
}
async function handleApiRequest(req, res, url) {
  const pathname = url.pathname;
  const method = req.method.toUpperCase();

  if (method === 'POST' && pathname === '/api/login') {
    const body = await parseBody(req);
    const rawEmail = body && typeof body.email === 'string' ? body.email : '';
    const rawPassword = body && typeof body.password === 'string' ? body.password : '';
    const sanitizedEmail = sanitizeEmailInput(rawEmail);
    const normalizedEmail = sanitizedEmail.toLowerCase();
    const sanitizedPassword = sanitizePasswordInput(rawPassword);

    if (!sanitizedEmail || !sanitizedPassword) {
      return respondJson(res, 400, { error: 'E-Mail und Passwort sind erforderlich.' });
    }

    if (!isValidEmailAddress(sanitizedEmail)) {
      return respondJson(res, 400, { error: 'Bitte geben Sie eine gültige E-Mail-Adresse ein.' });
    }

    if (sanitizedPassword.length > 256) {
      return respondJson(res, 400, { error: 'Das Passwort überschreitet die maximale Länge.' });
    }

    if (isSuspiciousLoginValue(rawEmail) || isSuspiciousLoginValue(rawPassword)) {
      console.warn('Suspicious login attempt blocked', {
        ip: req.socket?.remoteAddress,
        email: sanitizedEmail
      });
      return respondJson(res, 400, { error: 'Die Anmeldung konnte nicht verarbeitet werden.' });
    }

    const users = await readUsers();
    const user = users.find((u) => u.email.toLowerCase() === normalizedEmail);
    if (!user || !verifyPassword(sanitizedPassword, user.password)) {
      return respondJson(res, 401, { error: 'Ungültige Zugangsdaten.' });
    }
    const sessionId = createSession(user.id);
    setSessionCookie(res, sessionId);
    return respondJson(res, 200, { user: { id: user.id, name: user.name, email: user.email, role: user.role } });
  }

  if (method === 'POST' && pathname === '/api/logout') {
    const session = getSession(req);
    if (session) {
      sessions.delete(session.sessionId);
    }
    clearSessionCookie(res);
    return respondJson(res, 200, { success: true });
  }

  if (method === 'GET' && pathname === '/api/session') {
    const session = getSession(req);
    if (!session) {
      return respondJson(res, 200, { user: null });
    }
    const users = await readUsers();
    const user = users.find((u) => u.id === session.userId);
    if (!user) {
      sessions.delete(session.sessionId);
      clearSessionCookie(res);
      return respondJson(res, 200, { user: null });
    }
    return respondJson(res, 200, { user: { id: user.id, name: user.name, email: user.email, role: user.role } });
  }

  if (pathname.startsWith('/api/bookings')) {
    const bookings = await readBookings();
    const session = getSession(req);
    let currentUser = null;
    if (session) {
      const users = await readUsers();
      currentUser = users.find((u) => u.id === session.userId) || null;
      if (!currentUser) {
        sessions.delete(session.sessionId);
        clearSessionCookie(res);
      }
    }

    if (method === 'GET' && pathname === '/api/bookings') {
      const fromParam = url.searchParams.get('from');
      const toParam = url.searchParams.get('to');
      const fromDate = fromParam ? new Date(fromParam) : null;
      const toDate = toParam ? new Date(toParam) : null;
      const includeCancelled = currentUser && currentUser.role === 'admin';
      const filtered = bookings.filter((booking) => {
        const start = new Date(booking.startDateTime);
        const end = new Date(booking.endDateTime);
        if (!includeCancelled && booking.status === 'cancelled') {
          return false;
        }
        if (fromDate && !Number.isNaN(fromDate.valueOf()) && end < fromDate) {
          return false;
        }
        if (toDate && !Number.isNaN(toDate.valueOf()) && start > toDate) {
          return false;
        }
        return true;
      });

      if (currentUser && currentUser.role === 'admin') {
        return respondJson(res, 200, { bookings: filtered.map(mapAdminBooking) });
      }
      return respondJson(res, 200, { bookings: filtered.map(sanitizePublicBooking) });
    }

    if (method === 'POST' && pathname === '/api/bookings') {
      const contentType = req.headers['content-type'] || '';
      if (!contentType.includes('multipart/form-data')) {
        return respondJson(res, 415, {
          error: 'Bitte verwenden Sie das Formular mit Dateiupload für Praktikumsanfragen.'
        });
      }
      let parsedForm;
      try {
        parsedForm = await parseMultipartForm(req);
      } catch (error) {
        if (error.code === 'LIMIT_EXCEEDED') {
          return respondJson(res, 413, {
            error: 'Die Anfrage überschreitet die zulässige Upload-Größe von 25 MB.'
          });
        }
        return respondJson(res, 400, { error: 'Das Formular konnte nicht verarbeitet werden.' });
      }

      const fields = parsedForm.fields || {};
      const uploads = (parsedForm.files || []).filter(
        (file) => file && file.size && file.originalName
      );

      const name = getFieldValue(fields, 'name');
      const email = getFieldValue(fields, 'email');
      const phone = getFieldValue(fields, 'phone');
      const note = getFieldValue(fields, 'note');
      let startDateTime = getFieldValue(fields, 'startDateTime');
      let endDateTime = getFieldValue(fields, 'endDateTime');

      if (!startDateTime) {
        const startDate = getFieldValue(fields, 'startDate');
        const startTime = getFieldValue(fields, 'startTime');
        if (startDate && startTime) {
          startDateTime = `${startDate}T${startTime}`;
        }
      }

      if (!endDateTime) {
        const endDate = getFieldValue(fields, 'endDate');
        const endTime = getFieldValue(fields, 'endTime');
        if (endDate && endTime) {
          endDateTime = `${endDate}T${endTime}`;
        }
      }

      if (!name || !email || !startDateTime || !endDateTime) {
        return respondJson(res, 400, {
          error: 'Name, E-Mail sowie Start- und Endzeit sind erforderlich.'
        });
      }

      const start = new Date(startDateTime);
      const end = new Date(endDateTime);
      if (Number.isNaN(start.valueOf()) || Number.isNaN(end.valueOf())) {
        return respondJson(res, 400, { error: 'Ungültiges Datumsformat.' });
      }
      if (end <= start) {
        return respondJson(res, 400, { error: 'Die Endzeit muss nach der Startzeit liegen.' });
      }

      const overlaps = bookings.some((booking) => {
        if (booking.status === 'cancelled') {
          return false;
        }
        const existingStart = new Date(booking.startDateTime);
        const existingEnd = new Date(booking.endDateTime);
        return existingStart < end && existingEnd > start;
      });
      if (overlaps) {
        return respondJson(res, 409, { error: 'Der gewünschte Zeitraum ist bereits reserviert.' });
      }

      const hasCv = uploads.some((file) => file.fieldName === 'cv');
      const hasApplication = uploads.some((file) => file.fieldName === 'application');
      if (!hasCv || !hasApplication) {
        return respondJson(res, 400, {
          error: 'Bitte lade sowohl Lebenslauf als auch Anschreiben hoch.'
        });
      }

      const oversized = uploads.find((file) => file.size > MAX_BOOKING_FILE_SIZE);
      if (oversized) {
        return respondJson(res, 413, {
          error: `Die Datei "${oversized.originalName}" überschreitet die maximale Größe von 12 MB.`
        });
      }

      const bookingId = generateId();
      let attachments = [];
      try {
        attachments = await saveBookingAttachments(bookingId, uploads);
      } catch (error) {
        console.error('Anhänge konnten nicht gespeichert werden', error);
        return respondJson(res, 500, {
          error: 'Die Unterlagen konnten nicht gespeichert werden. Bitte versuchen Sie es erneut.'
        });
      }

      const newBooking = {
        id: bookingId,
        name: name,
        email: email,
        phone: phone,
        note: note,
        startDateTime: start.toISOString(),
        endDateTime: end.toISOString(),
        status: 'pending',
        createdAt: new Date().toISOString(),
        attachments
      };
      bookings.push(newBooking);
      await writeBookings(bookings);
      notifyBooking(newBooking).catch((error) =>
        console.error('Fehler beim Senden der Buchungsbenachrichtigung', error)
      );
      return respondJson(res, 201, {
        booking: sanitizePublicBooking(newBooking),
        message: 'Vielen Dank! Wir haben Ihre Anfrage erhalten.'
      });
    }

    if (method === 'GET') {
      const fileMatch = pathname.match(/^\/api\/bookings\/([^/]+)\/files\/([^/]+)$/);
      if (fileMatch) {
        if (!currentUser || currentUser.role !== 'admin') {
          return respondJson(res, 403, { error: 'Administratorberechtigung erforderlich.' });
        }
        const [, bookingId, fileId] = fileMatch;
        const booking = bookings.find((b) => b.id === bookingId);
        if (!booking) {
          return respondJson(res, 404, { error: 'Buchung nicht gefunden.' });
        }
        const attachment = Array.isArray(booking.attachments)
          ? booking.attachments.find((item) => item.id === fileId)
          : null;
        if (!attachment) {
          return respondJson(res, 404, { error: 'Anhang nicht gefunden.' });
        }
        const relativePath = attachment.path || '';
        const absolutePath = path.join(BOOKING_UPLOAD_DIR, relativePath);
        if (!absolutePath.startsWith(BOOKING_UPLOAD_DIR)) {
          return respondJson(res, 403, { error: 'Zugriff verweigert.' });
        }
        try {
          const stat = await fsp.stat(absolutePath);
          const downloadName = sanitizeFileName(attachment.originalName || 'Anhang') || 'Anhang';
          const stream = fs.createReadStream(absolutePath);
          stream.on('error', (error) => {
            console.error('Fehler beim Ausliefern eines Anhangs', error);
            if (!res.headersSent) {
              respondJson(res, 500, { error: 'Datei konnte nicht bereitgestellt werden.' });
            } else {
              res.destroy(error);
            }
          });
          res.writeHead(200, {
            'Content-Type': attachment.mimeType || 'application/octet-stream',
            'Content-Length': stat.size,
            'Content-Disposition': `attachment; filename="${downloadName}"`,
            'Cache-Control': 'private, max-age=0'
          });
          stream.pipe(res);
        } catch (error) {
          if (error.code === 'ENOENT') {
            return respondJson(res, 404, { error: 'Datei nicht gefunden.' });
          }
          console.error('Download fehlgeschlagen', error);
          return respondJson(res, 500, { error: 'Datei konnte nicht bereitgestellt werden.' });
        }
        return;
      }
    }

    if (!currentUser || currentUser.role !== 'admin') {
      return respondJson(res, 403, { error: 'Administratorberechtigung erforderlich.' });
    }

    const idMatch = pathname.match(/^\/api\/bookings\/(.+)$/);
    if (!idMatch) {
      return respondJson(res, 404, { error: 'Nicht gefunden.' });
    }
    const bookingId = idMatch[1];
    const index = bookings.findIndex((booking) => booking.id === bookingId);
    if (index === -1) {
      return respondJson(res, 404, { error: 'Buchung nicht gefunden.' });
    }

    if (method === 'PUT') {
      const body = await parseBody(req);
      const allowedStatus = ['pending', 'confirmed', 'cancelled', 'completed'];
      if (body.status && !allowedStatus.includes(body.status)) {
        return respondJson(res, 400, { error: 'Ungültiger Statuswert.' });
      }
      const booking = bookings[index];
      if (body.status) {
        booking.status = body.status;
      }
      if (body.note !== undefined) {
        booking.note = String(body.note).trim();
      }
      await writeBookings(bookings);
      return respondJson(res, 200, { booking: mapAdminBooking(booking) });
    }

    if (method === 'DELETE') {
      const booking = bookings[index];
      await removeBookingAttachments(booking).catch((error) => {
        console.warn('Fehler beim Entfernen von Anhängen:', error.message);
      });
      bookings.splice(index, 1);
      await writeBookings(bookings);
      return respondJson(res, 200, { success: true });
    }

    return respondJson(res, 405, { error: 'Methode nicht erlaubt.' });
  }

  if (pathname.startsWith('/api/downloads')) {
    const session = getSession(req);
    const downloads = await readDownloads();

    if (method === 'GET' && pathname === '/api/downloads') {
      return respondJson(res, 200, { downloads });
    }

    if (!session) {
      return respondJson(res, 401, { error: 'Bitte melden Sie sich an.' });
    }

    const users = await readUsers();
    const user = users.find((u) => u.id === session.userId);
    if (!user) {
      sessions.delete(session.sessionId);
      clearSessionCookie(res);
      return respondJson(res, 401, { error: 'Sitzung ungültig.' });
    }
    if (!['admin', 'editor'].includes(user.role)) {
      return respondJson(res, 403, { error: 'Keine Berechtigung.' });
    }

    if (method === 'POST' && pathname === '/api/downloads') {
      const body = await parseBody(req);
      const { title, version, description, releaseDate } = body;
      if (!title) {
        return respondJson(res, 400, { error: 'Titel ist erforderlich.' });
      }
      let filePath = body.filePath || null;
      if (body.file && body.fileName) {
        let base64 = body.file;
        if (base64.includes(',')) {
          base64 = base64.split(',').pop();
        }
        const buffer = Buffer.from(base64, 'base64');
        const finalName = `${Date.now()}-${sanitizeFileName(body.fileName)}`;
        const targetPath = path.join(DOWNLOAD_DIR, finalName);
        await fsp.writeFile(targetPath, buffer);
        filePath = `/downloads/${finalName}`;
      }
      const newEntry = {
        id: generateId(),
        title,
        version: version || '',
        description: description || '',
        releaseDate: releaseDate || '',
        filePath
      };
      downloads.push(newEntry);
      await writeDownloads(downloads);
      return respondJson(res, 201, { download: newEntry });
    }

    const idMatch = pathname.match(/^\/api\/downloads\/(.+)$/);
    if (!idMatch) {
      return respondJson(res, 404, { error: 'Nicht gefunden.' });
    }
    const downloadId = idMatch[1];
    const index = downloads.findIndex((d) => d.id === downloadId);
    if (index === -1) {
      return respondJson(res, 404, { error: 'Download nicht gefunden.' });
    }

    if (method === 'PUT') {
      const body = await parseBody(req);
      const download = downloads[index];
      if (body.title) {
        download.title = body.title;
      }
      download.version = body.version ?? download.version;
      download.description = body.description ?? download.description;
      download.releaseDate = body.releaseDate ?? download.releaseDate;
      if (body.file && body.fileName) {
        let base64 = body.file;
        if (base64.includes(',')) {
          base64 = base64.split(',').pop();
        }
        const buffer = Buffer.from(base64, 'base64');
        const finalName = `${Date.now()}-${sanitizeFileName(body.fileName)}`;
        const targetPath = path.join(DOWNLOAD_DIR, finalName);
        await fsp.writeFile(targetPath, buffer);
        if (download.filePath) {
          const existingPath = path.join(PUBLIC_DIR, download.filePath);
          try {
            await fsp.unlink(existingPath);
          } catch {
            // ignore errors when removing old file
          }
        }
        download.filePath = `/downloads/${finalName}`;
      }
      await writeDownloads(downloads);
      return respondJson(res, 200, { download });
    }

    if (method === 'DELETE') {
      const download = downloads[index];
      downloads.splice(index, 1);
      await writeDownloads(downloads);
      if (download.filePath) {
        const existingPath = path.join(PUBLIC_DIR, download.filePath);
        try {
          await fsp.unlink(existingPath);
        } catch {
          // ignore
        }
      }
      return respondJson(res, 200, { success: true });
    }

    return respondJson(res, 405, { error: 'Methode nicht erlaubt.' });
  }

  if (pathname.startsWith('/api/users')) {
    const session = getSession(req);
    if (!session) {
      return respondJson(res, 401, { error: 'Bitte melden Sie sich an.' });
    }
    const users = await readUsers();
    const currentUser = users.find((u) => u.id === session.userId);
    if (!currentUser || currentUser.role !== 'admin') {
      return respondJson(res, 403, { error: 'Administratorberechtigungen erforderlich.' });
    }

    if (method === 'GET' && pathname === '/api/users') {
      const safeUsers = users.map(({ password, ...rest }) => rest);
      return respondJson(res, 200, { users: safeUsers });
    }

    if (method === 'POST' && pathname === '/api/users') {
      const body = await parseBody(req);
      const { name, email, role, password } = body;
      if (!name || !email || !role || !password) {
        return respondJson(res, 400, { error: 'Name, E-Mail, Rolle und Passwort sind erforderlich.' });
      }
      if (users.some((u) => u.email.toLowerCase() === email.toLowerCase())) {
        return respondJson(res, 409, { error: 'Die E-Mail-Adresse wird bereits verwendet.' });
      }
      const newUser = {
        id: generateId(),
        name,
        email,
        role,
        password: hashPassword(password)
      };
      users.push(newUser);
      await writeUsers(users);
      const { password: _pw, ...safeUser } = newUser;
      return respondJson(res, 201, { user: safeUser });
    }

    const idMatch = pathname.match(/^\/api\/users\/(.+)$/);
    if (!idMatch) {
      return respondJson(res, 404, { error: 'Nicht gefunden.' });
    }
    const userId = idMatch[1];
    const index = users.findIndex((u) => u.id === userId);
    if (index === -1) {
      return respondJson(res, 404, { error: 'Benutzer nicht gefunden.' });
    }

    if (method === 'PUT') {
      const body = await parseBody(req);
      if (body.email) {
        const duplicate = users.find((u, idx) => u.email.toLowerCase() === body.email.toLowerCase() && idx !== index);
        if (duplicate) {
          return respondJson(res, 409, { error: 'Die E-Mail-Adresse wird bereits verwendet.' });
        }
      }
      const user = users[index];
      if (body.name) {
        user.name = body.name;
      }
      if (body.email) {
        user.email = body.email;
      }
      if (body.role) {
        const newRole = body.role;
        const wasAdmin = user.role === 'admin';
        const willBeAdmin = newRole === 'admin';
        if (wasAdmin && !willBeAdmin) {
          const otherAdmins = users.filter((u, idx) => idx !== index && u.role === 'admin');
          if (otherAdmins.length === 0) {
            return respondJson(res, 400, { error: 'Mindestens ein Administrator muss bestehen bleiben.' });
          }
        }
        user.role = newRole;
      }
      if (body.password) {
        user.password = hashPassword(body.password);
      }
      await writeUsers(users);
      const { password: _pw, ...safeUser } = user;
      return respondJson(res, 200, { user: safeUser });
    }

    if (method === 'DELETE') {
      if (users[index].id === currentUser.id) {
        return respondJson(res, 400, { error: 'Sie können Ihr eigenes Konto nicht löschen.' });
      }
      const userToDelete = users[index];
      if (userToDelete.role === 'admin') {
        const otherAdmins = users.filter((u, idx) => idx !== index && u.role === 'admin');
        if (otherAdmins.length === 0) {
          return respondJson(res, 400, { error: 'Mindestens ein Administrator muss bestehen bleiben.' });
        }
      }
      users.splice(index, 1);
      await writeUsers(users);
      return respondJson(res, 200, { success: true });
    }

    return respondJson(res, 405, { error: 'Methode nicht erlaubt.' });
  }

  respondJson(res, 404, { error: 'Endpunkt nicht gefunden.' });
}

async function serveFile(res, filePath) {
  try {
    const data = await fsp.readFile(filePath);
    const contentType = getContentType(filePath);
    res.writeHead(200, { 'Content-Type': contentType });
    res.end(data);
  } catch (error) {
    if (error.code === 'ENOENT') {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Datei nicht gefunden');
    } else {
      console.error(error);
      res.writeHead(500, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Serverfehler');
    }
  }
}

function getContentType(filePath) {
  const ext = path.extname(filePath).toLowerCase();
  switch (ext) {
    case '.html':
      return 'text/html; charset=utf-8';
    case '.css':
      return 'text/css; charset=utf-8';
    case '.js':
      return 'application/javascript; charset=utf-8';
    case '.json':
      return 'application/json; charset=utf-8';
    case '.png':
      return 'image/png';
    case '.jpg':
    case '.jpeg':
      return 'image/jpeg';
    case '.svg':
      return 'image/svg+xml';
    case '.pdf':
      return 'application/pdf';
    case '.zip':
      return 'application/zip';
    default:
      return 'application/octet-stream';
  }
}

async function handleAdminPage(req, res, targetFile) {
  const session = getSession(req);
  if (!session) {
    res.writeHead(302, { Location: '/admin/login' });
    res.end();
    return;
  }
  const users = await readUsers();
  const user = users.find((u) => u.id === session.userId);
  if (!user) {
    sessions.delete(session.sessionId);
    clearSessionCookie(res);
    res.writeHead(302, { Location: '/admin/login' });
    res.end();
    return;
  }
  await serveFile(res, targetFile);
}

async function handleRequest(req, res) {
  const url = new URL(req.url, `http://${req.headers.host}`);
  const pathname = url.pathname;

  if (pathname.startsWith('/api/')) {
    try {
      await handleApiRequest(req, res, url);
    } catch (error) {
      console.error(error);
      if (error && error.code === 'LIMIT_EXCEEDED') {
        respondJson(res, 413, { error: 'Die Anfrage ist zu groß.' });
        return;
      }
      respondJson(res, 500, { error: 'Unerwarteter Fehler.' });
    }
    return;
  }

  if (pathname === '/login') {
    res.writeHead(302, { Location: '/admin/login' });
    res.end();
    return;
  }

  if (pathname === '/admin/login') {
    await serveFile(res, path.join(PUBLIC_DIR, 'admin', 'login.html'));
    return;
  }

  if (pathname === '/admin' || pathname === '/admin/') {
    await handleAdminPage(req, res, path.join(PUBLIC_DIR, 'admin', 'dashboard.html'));
    return;
  }

  if (pageRoutes[pathname]) {
    await serveFile(res, path.join(PUBLIC_DIR, 'pages', pageRoutes[pathname]));
    return;
  }

  let requestedPath = pathname;
  if (requestedPath.startsWith('/public/')) {
    requestedPath = requestedPath.replace('/public', '');
  }
  const filePath = path.join(PUBLIC_DIR, requestedPath);
  if (!filePath.startsWith(PUBLIC_DIR)) {
    res.writeHead(403, { 'Content-Type': 'text/plain; charset=utf-8' });
    res.end('Zugriff verweigert');
    return;
  }

  try {
    const stat = await fsp.stat(filePath);
    if (stat.isDirectory()) {
      const indexPath = path.join(filePath, 'index.html');
      await serveFile(res, indexPath);
      return;
    }
    await serveFile(res, filePath);
  } catch (error) {
    if (error.code === 'ENOENT') {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Seite nicht gefunden');
    } else {
      console.error(error);
      res.writeHead(500, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Serverfehler');
    }
  }
}

async function start() {
  await ensureDataFiles();
  const server = http.createServer((req, res) => {
    handleRequest(req, res).catch((error) => {
      console.error('Unhandled error', error);
      res.writeHead(500, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Serverfehler');
    });
  });

  server.listen(PORT, HOST, () => {
    console.log(`Server läuft auf http://${HOST}:${PORT}`);
  });
}

start().catch((error) => {
  console.error('Failed to start server', error);
  process.exit(1);
});
