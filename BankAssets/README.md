# BankAssets – Anleitung

## Voraussetzungen
- .NET 8 SDK für Windows (WinForms)
- SQLite ist integriert (Microsoft.Data.Sqlite)

## Start der App
1. Projekt in Visual Studio öffnen und `BankAssets` als Startprojekt wählen.
2. App starten.
3. Beim ersten Start erscheint **„Datenbank einrichten“**. Wähle einen Ordner – die Datei `bank-assets.db` wird dort erstellt.

## Benötigte NuGet-Pakete
- `Microsoft.Data.Sqlite`

## Konto hinzufügen (inkl. Bank-Vorlagen)
1. In der Hauptansicht auf **„Offline-Konto hinzufügen“** klicken.
2. Eine **Bankvorlage** auswählen (z. B. „DKB“, „Sparkasse“) **oder** „Benutzerdefiniert“ wählen und den Banknamen manuell angeben.
3. Kontoname und IBAN eintragen.
4. Optional: **API-Zugangsdaten** hinterlegen (für Online-Banken / PSD2-Connectoren).
4. Speichern.

> Alle Daten werden in der SQLite-Datenbank gespeichert und verschlüsselt abgelegt.

## Transaktionen manuell hinzufügen
1. In der Kontoliste doppelklicken, um die Kontoübersicht zu öffnen.
2. Auf **„Transaktion hinzufügen“** klicken.
3. Betrag, Verwendungszweck, Gegenpartei und Datum eintragen.
4. Speichern.

## Filter für Transaktionen
- Im Konto-Detailfenster Zeitraum auswählen und **„Anwenden“** klicken.

## Online-Sync
Über **„Online-Sync starten“** in der Hauptansicht werden Banken mit `apiType` ≠ `offline` synchronisiert.  
Der Demo-Connector (`nordigen`) zeigt aktuell nur, **wo** ein echter Connector integriert wird.

## Bank-Schnittstellen (API) – falls verfügbar
Für echte Bank-APIs brauchst du in der Regel:
- **Client-ID / Client-Secret** (oder API-Token)
- **Redirect-URL** (für OAuth-Anmeldung)
- Freischaltung bei der jeweiligen Bank oder einem PSD2-Aggregator

### Woher bekomme ich die Zugangsdaten?
- **Direkt bei der Bank**: Viele Banken stellen nur für Geschäftskunden oder nach Registrierung eine API bereit.
- **PSD2-Aggregatoren** (kostenlos oft eingeschränkt):
  - *Nordigen/GoCardless* (Free Tier)
  - *Tink* (Developer Account)

### Wie implementiere ich die Schnittstelle?
1. In `Services/IBankConnector.vb` eine Implementierung hinzufügen (z. B. `NordigenConnector`).
2. Authentifizierung anhand der API-Dokumentation implementieren (Client Credentials + OAuth).
3. In `Services/BankSyncService.vb` den Connector anhand `apiType` registrieren.
4. Beim Erstellen der Bank `ApiType` entsprechend setzen (z. B. `nordigen`) und die Zugangsdaten im Dialog hinterlegen.

**Hinweis:** Die aktuelle App nutzt bereits das `apiType`-Feld in der Banktabelle, damit später eine echte Synchronisation ergänzt werden kann.
