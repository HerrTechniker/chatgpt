# HPS Power Solutions – Demo-Website

Diese Anwendung stellt eine mehrseitige Marketing-Website inklusive Administrationsbereich für Downloads und Benutzerverwaltung bereit. Sie orientiert sich inhaltlich an der bestehenden Webseite des Kunden (hps-power.com), bietet jedoch eine eigenständige Umsetzung mit modernen Oberflächenkomponenten.

## Features

- **Mehrseitige Website** (Startseite, Lösungen, Service, Branchen, Downloads, Unternehmen, Kontakt, Praktika)
- **Buchungskalender für Praktika** mit Verfügbarkeitsübersicht, Anfrageformular und Bestätigungs-E-Mails
- **Download-Bereich** mit dynamischer Liste, die vom Admin-Interface gepflegt wird
- **Admin-Dashboard**
  - Upload & Austausch von Dateien (z. B. neue Software-Versionen)
  - Verwaltung von Metadaten (Titel, Version, Beschreibung, Veröffentlichungsdatum)
  - Benutzerverwaltung (Rollen „Admin“ & „Editor“)
  - Praktikumsanfragen einsehen, Status ändern und löschen
- **Dateibasierte Persistenz** über JSON-Dateien im `data/`-Ordner
- **Minimale Abhängigkeiten** – lediglich [Nodemailer](https://nodemailer.com/) für den SMTP-Versand

## Voraussetzungen

- Node.js 18 oder höher

## Installation & Start

```bash
npm install
node app.js
```

Der Server startet anschließend auf `http://0.0.0.0:3000` (Port kann über `PORT`-Umgebungsvariable gesetzt werden).

### E-Mail-Konfiguration

Für den Versand der Eingangsbestätigungen werden folgende Umgebungsvariablen unterstützt (optional, ohne Konfiguration wird der Versand im Log protokolliert):

- `SMTP_HOST` – Adresse des SMTP-Servers (erforderlich für Versand)
- `SMTP_PORT` – Port (Standard: `587`)
- `SMTP_SECURE` – `true`, wenn TLS erforderlich ist (Standard: `false`)
- `SMTP_USER` / `SMTP_PASS` – Anmeldedaten für den SMTP-Server (optional)
- `MAIL_SENDER` – Absenderadresse der E-Mails (Standard: `HPS Power Solutions <info@hps-power.local>`)
- `COMPANY_NOTIFICATION_EMAIL` – interne Benachrichtigungsadresse (Standard: `info@hps-power.local`)

## Standard-Zugangsdaten

Beim ersten Start wird automatisch ein Administrationskonto angelegt:

- **E-Mail:** `admin@hps-power.local`
- **Passwort:** `ChangeMe123!`

Bitte ändern Sie das Passwort nach dem ersten Login über die Benutzerverwaltung.

## Dateistruktur

```
app.js                 # HTTP-Server, Routing und REST-API
public/                # Statische Website-Dateien
  pages/               # Mehrseitige HTML-Seiten
  css/style.css        # Globales Styling inkl. Adminbereich
  js/                  # Frontend-Skripte
  admin/               # Admin-spezifische Seiten & Scripts
  downloads/           # Upload-Ziel für Dateien aus dem Adminbereich
data/
  users.json           # Benutzer (inkl. Passwort-Hash)
  downloads.json       # Download-Einträge
  bookings.json        # Praktikumsanfragen
```

## Rollen & Berechtigungen

- **Admin**: Vollzugriff auf Downloads & Benutzerverwaltung.
- **Editor**: Darf Downloads anlegen/ändern/löschen, hat jedoch keinen Zugriff auf die Benutzerverwaltung.

## Sicherheitshinweise

- Passwörter werden mit Node.js `crypto.scrypt` gehasht.
- Sessions werden im Speicher verwaltet und über HTTP-only Cookies gesichert.
- Uploads landen automatisch im Ordner `public/downloads` und werden bei Bedarf ersetzt.
- Löschen eines Admins ist nur möglich, wenn mindestens ein weiterer Admin existiert.

## Weiterführende Anpassungen

- Deployment via `pm2`, Docker oder systemd
- Einbindung echter Karten (z. B. Leaflet/Mapbox)
- Erweiterung des Download-Modells um Changelogs oder Release-Notes
- Anbindung an bestehende Authentifizierungs- oder CRM-Systeme

Viel Erfolg mit der neuen Demo-Seite!
