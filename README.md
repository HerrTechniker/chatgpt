# SQLite Bibliothek für Visual Basic

Diese Bibliothek stellt grundlegende Helferfunktionen rund um SQLite bereit und basiert auf dem NuGet-Paket [`System.Data.SQLite.Core`](https://www.nuget.org/packages/System.Data.SQLite.Core/).

## Installation

1. Neues Visual-Basic-Projekt erstellen (z. B. Class Library).
2. Das NuGet-Paket `System.Data.SQLite.Core` hinzufügen:

   ```bash
   dotnet add package System.Data.SQLite.Core
   ```

3. Die Datei `SQLiteBibliothek.vb` in das Projekt aufnehmen.
4. Die Funktionen des `SqliteBibliothek`-Moduls in Ihrem Code aufrufen.

## Funktionsübersicht

| Funktion | Beschreibung |
| --- | --- |
| `ErstelleVerbindungsstring` | Erstellt einen SQLite-Verbindungsstring und legt fehlende Verzeichnisse an. |
| `ErstelleDatenbank` | Legt die Datenbankdatei sowie die erforderliche `sqlite_sequence`-Tabelle an. |
| `LoescheDatenbank` | Entfernt eine Datenbankdatei. |
| `ErstelleTabelle` | Erstellt eine Tabelle mit `Id` als erster Spalte (`INTEGER PRIMARY KEY`). |
| `FuegeDatensatzEin` | Fügt einen Datensatz ein und gibt die automatisch vergebene Id zurück. |
| `AktualisiereDatensatz` | Aktualisiert einen vorhandenen Datensatz über seine Id. |
| `LoescheDatensatz` | Löscht einen Datensatz anhand seiner Id. |
| `HoleDatensatz` | Liest einen einzelnen Datensatz und gibt ihn als Dictionary zurück. |
| `HoleAlleDatensaetze` | Liest alle Datensätze einer Tabelle in einer Liste von Dictionaries aus. |
| `HoleLetzteId` | Gibt die zuletzt vergebene Id aus der `sqlite_sequence`-Tabelle zurück. |

Alle Operationen verwenden parametrisierte SQL-Befehle und aktualisieren automatisch die Tabelle `sqlite_sequence`, sodass die Id-Spalte eindeutig bleibt.
