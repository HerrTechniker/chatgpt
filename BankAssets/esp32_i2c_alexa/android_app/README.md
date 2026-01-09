# Android App (ESP32 RGB Controller)

Diese Android-App bietet eine eigene Oberfläche (ohne WebView) und enthält einen Setup‑Screen, der das lokale Netzwerk nach einem ESP32 sucht.

## Funktionen

- Setup‑Screen mit Netzwerkscan (ESP32 automatisch finden)
- Fallback: Broadcast‑Discovery, falls der normale Scan kein Gerät findet
- Automatische Erkennung von ESP32‑Geräten über `/api/state`
- Steuerung von LED‑Knoten (RGB per Slider, On/Off)
- Effektwahl (solid, flicker, rainbow, pulse)

## Build

1. Ordner `android_app/` in Android Studio öffnen.
2. Gradle sync ausführen.
3. App auf einem Gerät (Android 9+) starten.

> Hinweis: Scan benötigt eine aktive WLAN‑Verbindung. Wähle ein gefundenes Gerät aus und tippe auf „Hinzufügen“.
