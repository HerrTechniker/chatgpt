# ESP32-WROOM-32 + Alexa + I2C RGB-Controller

Dieses Beispiel besteht aus mehreren Bausteinen:

- `esp32_controller.ino`: Läuft auf dem ESP32, emuliert eine Philips-Hue-Bridge (Alexa kompatibel), bietet eine Web-API/Web-UI (Handy) und steuert RGB-Werte per I2C.
- `atmega_rgb_node.ino`: Läuft auf einem ATmega328, nimmt I2C-Kommandos an, speichert die letzte Farbe in EEPROM und steuert eine 5mm-RGB-LED (mit Vorwiderständen!).
- `pc_app/`: Beispiel einer Windows-Desktop-App (Visual Basic), die per HTTP mit dem ESP32 synchronisiert.

## Hardware

- ESP32-WROOM-32
- Mehrere Arduino ATmega328 (z. B. Nano/Uno oder Standalone)
- I2C Pullups (4.7k–10k auf SDA/SCL)
- RGB-LED 5mm + Vorwiderstände (z. B. 220–330 Ohm pro Kanal)

## I2C-Adressvergabe (Wichtig)

Der ESP32 kann **I2C-Adressen zuweisen**, aber I2C kann nicht mehrere Geräte mit identischer Adresse gleichzeitig verwalten.
Daher gilt folgende **Vorgehensweise**:

1. Ein ATmega wird erstmalig gestartet (ohne gespeicherte Adresse in EEPROM).
2. Der ESP32 sendet eine **General Call**-Nachricht (Adresse `0x00`) mit einer neuen Zieladresse.
3. Der ATmega speichert die neue Adresse in EEPROM und startet den I2C-Slave neu.
4. **Nur ein unzugewiesener ATmega gleichzeitig einschalten**, damit keine Kollisionen entstehen.

Danach kennt jeder ATmega seine eigene Adresse dauerhaft.

> Hinweis: Der ESP32 ist I2C-Master und benötigt keine eigene Slave-Adresse. Die Adresse `0x08` ist als „erste Adresse“ reserviert, die Nodes starten bei `0x09`.

## Alexa Einrichtung

Der ESP32 emuliert eine Hue-Bridge über die Bibliothek `fauxmoESP`.

1. ESP32 mit WLAN verbinden.
2. In der Alexa-App Geräte suchen ("Gerät hinzufügen" → "Licht" → "Philips Hue" → "Geräte suchen").
3. Alexa findet virtuelle Lichter wie `RGB Node 1`, `RGB Node 2`, ...
4. Helligkeit steuert die LED-Intensität.

## Bibliotheken

- `fauxmoESP` (Alexa Hue-Emulation)
- `ArduinoJson` (optional, wenn erweitert)

## PC-App (Visual Basic)

Die Beispiel-App liegt unter `pc_app/` und nutzt die REST-API des ESP32.

1. Projekt mit Visual Studio öffnen (`RgbControllerApp.vbproj`).
2. Starten und die ESP32-IP/Hostname eintragen (z. B. `http://esp32.local:8080`).
3. Farbe, Effekt und Profile setzen.

## Effekte

- **Solid**, **Flicker**, **Rainbow**, **Pulse** sind integriert.
- Effekte laufen auf dem ESP32 und werden zyklisch an die Nodes gesendet.

## Synchronisation (Handy + PC)

- Beide Apps sprechen die REST-API des ESP32 an (`http://<ip>:8080/api/state`, `/api/node`, `/api/effect`).
- Beispiel: Alle 3 Sekunden wird der Zustand geladen und UI aktualisiert.
- Änderung in einer App wird sofort an den ESP32 geschickt und erscheint in der anderen App nach dem nächsten Poll.

## WLAN-Einrichtung (ähnlich Shelly)

1. Wenn noch kein WLAN gespeichert ist, öffnet der ESP32 einen Hotspot **ESP32-RGB-Setup**.
2. Mit dem Handy verbinden und im Browser `http://192.168.4.1:8080` öffnen.
3. SSID/Passwort speichern → ESP32 startet neu und verbindet sich mit dem WLAN.

## Hinweise

- Für jede RGB-LED braucht es PWM-Pins am ATmega (z. B. D3, D5, D6).
- Ohne Vorwiderstände zerstört man die LED.
- Die Beispiel-Farbwerte sind fest (Rot). Du kannst Farbsteuerung erweitern (HSV, RGB, etc.).

## Dateien

- `esp32_controller.ino`
- `atmega_rgb_node.ino`
- `pc_app/`
