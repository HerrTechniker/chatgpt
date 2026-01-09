# Android App (ESP32 RGB Controller)

Diese Android-App bietet eine eigene Oberfläche (ohne WebView) und enthält einen Setup‑Screen, der das lokale Netzwerk nach einem ESP32 sucht.

## Funktionen

- Setup‑Screen mit Netzwerkscan und manueller IP/URL
- Automatische Erkennung von ESP32‑Geräten über `/api/state`
- Steuerung von LED‑Knoten (RGB per Slider, On/Off)
- Effektwahl (solid, flicker, rainbow, pulse)

## Build

1. Ordner `android_app/` in Android Studio öffnen.
2. Gradle sync ausführen.
3. App auf einem Gerät (Android 9+) starten.

> Hinweis: Scan benötigt eine aktive WLAN‑Verbindung. Falls kein Gerät gefunden wird, kann die IP manuell eingetragen werden.
