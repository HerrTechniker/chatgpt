# HPS Power Solutions – Demo-Website

Diese Anwendung stellt eine mehrseitige Marketing-Website inklusive Administrationsbereich für Downloads und Benutzerverwaltung bereit. Sie orientiert sich inhaltlich an der bestehenden Webseite des Kunden (hps-power.com), bietet jedoch eine eigenständige Umsetzung mit modernen Oberflächenkomponenten.

## Features

- **Mehrseitige Website** (Startseite, Lösungen, Service, Branchen, Downloads, Unternehmen, Kontakt, Praktika)
- **Eigenständige Impressum- und Datenschutzseiten** zur Übernahme der bestehenden Rechtstexte
- **Buchungskalender für Praktika** mit Verfügbarkeitsübersicht, Anfrageformular und Bestätigungs-E-Mails
- **Download-Bereich** mit dynamischer Liste, die vom Admin-Interface gepflegt wird
- **360° Firmenrundgang** auf `/de/virtual-tour` bzw. `/en/virtual-tour` mit Photo Sphere Viewer
- **Admin-Dashboard**
  - Upload & Austausch von Dateien (z. B. neue Software-Versionen)
  - Verwaltung von Metadaten (Titel, Version, Beschreibung, Veröffentlichungsdatum)
  - Benutzerverwaltung (Rollen „Admin“ & „Editor“)
  - Praktikumsanfragen einsehen, Status ändern und löschen
- **Dateibasierte Persistenz** über JSON-Dateien im `data/`-Ordner
- **Minimale Abhängigkeiten** – lediglich [Nodemailer](https://nodemailer.com/) für den SMTP-Versand
- **Legacy-Spiegelung** – Skript zum Herunterladen der bestehenden hps-power.com-Inhalte (Texte & Medien) für einen 1:1-Auftritt

## Voraussetzungen

- Node.js 18 oder höher

## Installation & Start

```bash
npm install
node app.js
```

Der Server startet anschließend auf `http://0.0.0.0:3000` (Port kann über `PORT`-Umgebungsvariable gesetzt werden). Die
deutschsprachige Website ist unter `http://<host>:<port>/de/`, die englische unter `http://<host>:<port>/en/` erreichbar.

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
  legacy/              # Automatisch gespiegelte Inhalte der Bestandsseite
  css/style.css        # Globales Styling inkl. Adminbereich
  js/                  # Frontend-Skripte
  admin/               # Admin-spezifische Seiten & Scripts
  downloads/           # Upload-Ziel für Dateien aus dem Adminbereich
data/
  users.json           # Benutzer (inkl. Passwort-Hash)
  downloads.json       # Download-Einträge
  bookings.json        # Praktikumsanfragen
```

## Originalinhalte spiegeln (1:1)

Damit sämtliche Texte und Medien exakt der bestehenden Seite entsprechen, ist ein Synchronisationsskript enthalten. Dieses lädt
die gewünschten Seiten, Assets (Bilder, Stylesheets, Downloads) herunter und legt sie unter `public/legacy/` ab. Der Server
priorisiert anschließend automatisch diese gespiegelten Dateien gegenüber den generischen Platzhaltern.

```bash
# vollständige Spiegelung (empfohlen)
npm run sync:legacy

# optional: alternative Domain oder Testsystem
npm run sync:legacy -- https://staging.hps-power.com

# Beispiel: nur eine Ebene tief crawlen
LEGACY_MAX_DEPTH=1 npm run sync:legacy

# vorhandene Dateien erhalten (keine automatische Löschung)
LEGACY_SKIP_CLEAN=true npm run sync:legacy
```

> **Hinweis:** Der Download erfordert einen direkten HTTP/HTTPS-Zugriff auf `hps-power.com`. In abgeschotteten Umgebungen oder
ohne Internetzugang schlägt der Abruf fehl. In diesem Fall können Sie die HTML- und Asset-Dateien manuell in `public/legacy/`
ablegen; die Anwendung erkennt sie automatisch. Nach erfolgreicher Synchronisation enthält `public/legacy/legacy-manifest.json`
eine Übersicht der importierten Seiten.

## Rechtstexte übernehmen

Nach der Spiegelung empfiehlt es sich, die importierten Fassungen von Impressum und Datenschutz zu prüfen und ggf. um
ergänzende juristische Hinweise zu erweitern. Die ursprünglichen Platzhalter-Dateien in `public/pages/` verbleiben als Fallback
und können für Test- oder Entwurfsinhalte genutzt werden.

## Rollen & Berechtigungen

- **Admin**: Vollzugriff auf Downloads & Benutzerverwaltung.
- **Editor**: Darf Downloads anlegen/ändern/löschen, hat jedoch keinen Zugriff auf die Benutzerverwaltung.

## Sicherheitshinweise

- Passwörter werden mit Node.js `crypto.scrypt` gehasht.
- Sessions werden im Speicher verwaltet und über HTTP-only Cookies gesichert.
- Uploads landen automatisch im Ordner `public/downloads` und werden bei Bedarf ersetzt.
- Löschen eines Admins ist nur möglich, wenn mindestens ein weiterer Admin existiert.
- Die Admin-Anmeldung validiert Eingaben server- und clientseitig, blockiert typische SQL-Injection-Muster und protokolliert verdächtige Versuche.

## Virtueller Rundgang mit der Insta360 X4

Die Seite `/de/virtual-tour` (englisch: `/en/virtual-tour`) bindet einen interaktiven 360°-Viewer ein, der standardmäßig ein Platzhalter-Panorama lädt. Um Ihre eigenen Insta360 X4 Aufnahmen zu veröffentlichen, gehen Sie wie folgt vor:

1. Exportieren Sie in Insta360 Studio ein equirektangulares JPG/PNG (Seitenverhältnis 2:1, z. B. 7680×3840 px).
2. Speichern Sie die Datei im Projektverzeichnis unter `public/tour/` und vergeben Sie einen sprechenden Namen (z. B. `werksrundgang.jpg`).
3. Passen Sie in `public/pages/virtual-tour.html` (bzw. der englischen Variante) das `data-panorama`-Attribut auf den neuen Pfad an, etwa `/tour/werksrundgang.jpg`.
   Der ausgelieferte Platzhalter ist jetzt als Data-URI im Skript `public/js/virtual-tour.js` hinterlegt – sobald Sie einen echten Dateipfad setzen, wird er automatisch überschrieben.
4. Nach dem Deployment lädt der Photo-Sphere-Viewer die Datei automatisch; die Statusmeldung auf der Seite signalisiert, ob der Rundgang bereit ist.

> **Tipp:** Wenn Sie eine gehostete Insta360-Webtour bevorzugen, können Sie den Viewer im HTML durch ein `<iframe>` ersetzen. Hinterlegen Sie dazu im Abschnitt "tour-viewer-card" anstelle des `div` eine Einbettung wie sie Insta360 bereitstellt.

## Weiterführende Anpassungen

- Deployment via `pm2`, Docker oder systemd
- Einbindung echter Karten (z. B. Leaflet/Mapbox)
- Erweiterung des Download-Modells um Changelogs oder Release-Notes
- Anbindung an bestehende Authentifizierungs- oder CRM-Systeme

Viel Erfolg mit der neuen Demo-Seite!
