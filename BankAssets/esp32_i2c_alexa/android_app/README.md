# Android App (ESP32 RGB Controller)

Ein minimales Android-Client-Projekt (Android 9 / API 28+), das die Web-UI des ESP32 in einer `WebView` anzeigt.

## Funktionen

- URL-Eingabe (z. B. `http://esp32.local:8080`)
- Öffnet die ESP32-Weboberfläche und übernimmt die Synchronisation über die vorhandene Web-UI.

## Build

1. Ordner `android_app/` in Android Studio öffnen.
2. Gradle sync ausführen.
3. App auf einem Gerät (Android 9+) starten.

> Hinweis: Die Web-UI muss erreichbar sein. Gegebenenfalls im gleichen WLAN bleiben.
