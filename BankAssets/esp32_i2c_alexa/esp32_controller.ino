#include <Arduino.h>
#include <WiFi.h>
#include <WebServer.h>
#include <Wire.h>
#include <Preferences.h>
#include <fauxmoESP.h>

// ===== WLAN / Setup =====
constexpr char SETUP_SSID[] = "ESP32-RGB-Setup";
constexpr char SETUP_PASS[] = "esp32setup";
constexpr char PREFS_NAMESPACE[] = "rgbctrl";
constexpr char PREF_WIFI_SSID[] = "wifi_ssid";
constexpr char PREF_WIFI_PASS[] = "wifi_pass";
constexpr char PREF_PROFILES[] = "profiles";

// ===== I2C =====
constexpr uint8_t I2C_SDA = 21;
constexpr uint8_t I2C_SCL = 22;
constexpr uint32_t I2C_SPEED = 400000;

// ===== I2C Protokoll =====
// 0x00 = General Call (Adressvergabe)
// 0x10 = RGB-Set (3 Bytes: R, G, B)
// 0x11 = Off (LED aus)
constexpr uint8_t CMD_SET_RGB = 0x10;
constexpr uint8_t CMD_SET_OFF = 0x11;
constexpr uint8_t CMD_ASSIGN_ADDRESS = 0xA0; // Payload: 1 Byte neue Adresse

// ===== Geräte =====
// Der ESP32 selbst benötigt keine I2C-Adresse, 0x08 bleibt reserviert.
// Liste der zugewiesenen Adressen (Beispiel: 0x09, 0x0A, 0x0B ...)
uint8_t nodeAddresses[] = {0x09, 0x0A, 0x0B};
constexpr size_t NODE_COUNT = sizeof(nodeAddresses) / sizeof(nodeAddresses[0]);

fauxmoESP fauxmo;
WebServer server(8080);
Preferences prefs;

struct NodeState {
  uint8_t r;
  uint8_t g;
  uint8_t b;
  bool on;
};

enum class EffectMode : uint8_t {
  Solid = 0,
  Flicker = 1,
  Rainbow = 2,
  Pulse = 3,
};

NodeState nodes[NODE_COUNT];
EffectMode currentEffect = EffectMode::Solid;
bool apMode = false;
unsigned long lastEffectTick = 0;
uint16_t rainbowHue = 0;

static void sendRgb(uint8_t address, uint8_t r, uint8_t g, uint8_t b) {
  Wire.beginTransmission(address);
  Wire.write(CMD_SET_RGB);
  Wire.write(r);
  Wire.write(g);
  Wire.write(b);
  Wire.endTransmission();
}

static void sendOff(uint8_t address) {
  Wire.beginTransmission(address);
  Wire.write(CMD_SET_OFF);
  Wire.endTransmission();
}

// Vergibt eine neue Adresse an einen frisch gestarteten ATmega (nur einer gleichzeitig!)
static void assignAddress(uint8_t newAddress) {
  Wire.beginTransmission(0x00); // General Call
  Wire.write(CMD_ASSIGN_ADDRESS);
  Wire.write(newAddress);
  Wire.endTransmission();
}

static void applyNodeState(size_t index) {
  if (index >= NODE_COUNT) {
    return;
  }

  uint8_t address = nodeAddresses[index];
  if (!nodes[index].on) {
    sendOff(address);
    return;
  }

  sendRgb(address, nodes[index].r, nodes[index].g, nodes[index].b);
}

static void applyAllNodes() {
  for (size_t i = 0; i < NODE_COUNT; ++i) {
    applyNodeState(i);
  }
}

static void setAll(uint8_t r, uint8_t g, uint8_t b) {
  for (size_t i = 0; i < NODE_COUNT; ++i) {
    nodes[i].r = r;
    nodes[i].g = g;
    nodes[i].b = b;
    nodes[i].on = true;
  }
  applyAllNodes();
}

static void hsvToRgb(uint16_t hue, uint8_t sat, uint8_t val, uint8_t &outR, uint8_t &outG, uint8_t &outB) {
  uint8_t region = hue / 60;
  uint16_t remainder = (hue - (region * 60)) * 255 / 60;

  uint8_t p = (val * (255 - sat)) / 255;
  uint8_t q = (val * (255 - ((sat * remainder) / 255))) / 255;
  uint8_t t = (val * (255 - ((sat * (255 - remainder)) / 255))) / 255;

  switch (region) {
    case 0:
      outR = val; outG = t; outB = p; break;
    case 1:
      outR = q; outG = val; outB = p; break;
    case 2:
      outR = p; outG = val; outB = t; break;
    case 3:
      outR = p; outG = q; outB = val; break;
    case 4:
      outR = t; outG = p; outB = val; break;
    default:
      outR = val; outG = p; outB = q; break;
  }
}

static void tickEffects() {
  unsigned long now = millis();
  if (now - lastEffectTick < 80) {
    return;
  }
  lastEffectTick = now;

  switch (currentEffect) {
    case EffectMode::Solid:
      return;
    case EffectMode::Flicker:
      for (size_t i = 0; i < NODE_COUNT; ++i) {
        if (!nodes[i].on) {
          continue;
        }
        uint8_t jitter = random(120, 255);
        sendRgb(nodeAddresses[i], (nodes[i].r * jitter) / 255, (nodes[i].g * jitter) / 255, (nodes[i].b * jitter) / 255);
      }
      return;
    case EffectMode::Rainbow: {
      rainbowHue = (rainbowHue + 3) % 360;
      for (size_t i = 0; i < NODE_COUNT; ++i) {
        if (!nodes[i].on) {
          continue;
        }
        uint16_t hue = (rainbowHue + (i * 360 / NODE_COUNT)) % 360;
        uint8_t r, g, b;
        hsvToRgb(hue, 255, 180, r, g, b);
        sendRgb(nodeAddresses[i], r, g, b);
      }
      return;
    }
    case EffectMode::Pulse: {
      uint8_t level = static_cast<uint8_t>((sin(now / 400.0) + 1.0) * 120.0);
      for (size_t i = 0; i < NODE_COUNT; ++i) {
        if (!nodes[i].on) {
          continue;
        }
        sendRgb(nodeAddresses[i], (nodes[i].r * level) / 255, (nodes[i].g * level) / 255, (nodes[i].b * level) / 255);
      }
      return;
    }
  }
}

static String effectName() {
  switch (currentEffect) {
    case EffectMode::Solid:
      return "solid";
    case EffectMode::Flicker:
      return "flicker";
    case EffectMode::Rainbow:
      return "rainbow";
    case EffectMode::Pulse:
      return "pulse";
  }
  return "solid";
}

static void setEffectByName(const String &name) {
  if (name == "flicker") {
    currentEffect = EffectMode::Flicker;
  } else if (name == "rainbow") {
    currentEffect = EffectMode::Rainbow;
  } else if (name == "pulse") {
    currentEffect = EffectMode::Pulse;
  } else {
    currentEffect = EffectMode::Solid;
  }
}

static void handleState() {
  String json = "{";
  json += "\"effect\":\"" + effectName() + "\",";
  json += "\"nodes\":[";
  for (size_t i = 0; i < NODE_COUNT; ++i) {
    json += "{";
    json += "\"r\":" + String(nodes[i].r) + ",";
    json += "\"g\":" + String(nodes[i].g) + ",";
    json += "\"b\":" + String(nodes[i].b) + ",";
    json += "\"on\":" + String(nodes[i].on ? "true" : "false");
    json += "}";
    if (i + 1 < NODE_COUNT) {
      json += ",";
    }
  }
  json += "]}";
  server.send(200, "application/json", json);
}

static void handleNodeUpdate() {
  if (!server.hasArg("node")) {
    server.send(400, "text/plain", "missing node");
    return;
  }

  size_t node = static_cast<size_t>(server.arg("node").toInt());
  if (node >= NODE_COUNT) {
    server.send(400, "text/plain", "invalid node");
    return;
  }

  if (server.hasArg("r")) nodes[node].r = static_cast<uint8_t>(server.arg("r").toInt());
  if (server.hasArg("g")) nodes[node].g = static_cast<uint8_t>(server.arg("g").toInt());
  if (server.hasArg("b")) nodes[node].b = static_cast<uint8_t>(server.arg("b").toInt());
  if (server.hasArg("on")) nodes[node].on = server.arg("on") == "1" || server.arg("on") == "true";

  applyNodeState(node);
  handleState();
}

static void handleEffectUpdate() {
  if (!server.hasArg("effect")) {
    server.send(400, "text/plain", "missing effect");
    return;
  }
  setEffectByName(server.arg("effect"));
  handleState();
}

static String getProfiles() {
  return String(prefs.getString(PREF_PROFILES, ""));
}

static void setProfiles(const String &profiles) {
  prefs.putString(PREF_PROFILES, profiles);
}

static void handleProfileSave() {
  if (!server.hasArg("name")) {
    server.send(400, "text/plain", "missing name");
    return;
  }
  String name = server.arg("name");
  String key = "profile_" + name;

  String payload = effectName();
  for (size_t i = 0; i < NODE_COUNT; ++i) {
    payload += ",";
    payload += String(nodes[i].r);
    payload += ":";
    payload += String(nodes[i].g);
    payload += ":";
    payload += String(nodes[i].b);
    payload += ":";
    payload += String(nodes[i].on ? 1 : 0);
  }

  prefs.putString(key.c_str(), payload);

  String existing = getProfiles();
  if (existing.indexOf(name) == -1) {
    if (existing.length() > 0) {
      existing += ",";
    }
    existing += name;
    setProfiles(existing);
  }

  server.send(200, "text/plain", "ok");
}

static void handleProfileLoad() {
  if (!server.hasArg("name")) {
    server.send(400, "text/plain", "missing name");
    return;
  }

  String key = "profile_" + server.arg("name");
  String payload = prefs.getString(key.c_str(), "");
  if (payload.length() == 0) {
    server.send(404, "text/plain", "not found");
    return;
  }

  int start = payload.indexOf(',');
  String effect = payload.substring(0, start);
  setEffectByName(effect);

  size_t idx = 0;
  int cursor = start + 1;
  while (cursor > 0 && idx < NODE_COUNT) {
    int next = payload.indexOf(',', cursor);
    String part = next >= 0 ? payload.substring(cursor, next) : payload.substring(cursor);
    int p1 = part.indexOf(':');
    int p2 = part.indexOf(':', p1 + 1);
    int p3 = part.indexOf(':', p2 + 1);
    if (p1 > 0 && p2 > p1 && p3 > p2) {
      nodes[idx].r = static_cast<uint8_t>(part.substring(0, p1).toInt());
      nodes[idx].g = static_cast<uint8_t>(part.substring(p1 + 1, p2).toInt());
      nodes[idx].b = static_cast<uint8_t>(part.substring(p2 + 1, p3).toInt());
      nodes[idx].on = part.substring(p3 + 1).toInt() == 1;
    }
    idx++;
    if (next < 0) {
      break;
    }
    cursor = next + 1;
  }

  applyAllNodes();
  handleState();
}

static void handleProfilesList() {
  server.send(200, "application/json", "{\"profiles\":\"" + getProfiles() + "\"}");
}

static void handleSetupPage() {
  String html = "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>"
                "<title>ESP32 Setup</title></head><body>"
                "<h1>WLAN Setup</h1>"
                "<form method='POST' action='/setup'>"
                "SSID:<br><input name='ssid'><br>"
                "Passwort:<br><input name='pass' type='password'><br><br>"
                "<button type='submit'>Speichern</button></form></body></html>";
  server.send(200, "text/html", html);
}

static void handleSetupPost() {
  if (!server.hasArg("ssid")) {
    server.send(400, "text/plain", "missing ssid");
    return;
  }
  prefs.putString(PREF_WIFI_SSID, server.arg("ssid"));
  prefs.putString(PREF_WIFI_PASS, server.arg("pass"));
  server.send(200, "text/plain", "Gespeichert. Neustart...");
  delay(500);
  ESP.restart();
}

static void handleControlPage() {
  String html = "<!doctype html><html><head><meta charset='utf-8'>"
                "<meta name='viewport' content='width=device-width, initial-scale=1'>"
                "<title>ESP32 RGB</title></head><body>"
                "<h1>RGB Controller</h1>"
                "<label>Node: <select id='node'></select></label><br><br>"
                "<input type='color' id='color' value='#ff0000'>"
                "<button onclick='applyColor()'>Setzen</button>"
                "<button onclick='setOff()'>Aus</button><br><br>"
                "<label>Effekt: <select id='effect'>"
                "<option value='solid'>Solid</option>"
                "<option value='flicker'>Flicker</option>"
                "<option value='rainbow'>Rainbow</option>"
                "<option value='pulse'>Pulse</option>"
                "</select></label>"
                "<button onclick='applyEffect()'>Effekt setzen</button><br><br>"
                "<label>Profilname: <input id='profile'></label>"
                "<button onclick='saveProfile()'>Speichern</button>"
                "<button onclick='loadProfile()'>Laden</button>"
                "<script>"
                "const nodeCount=" + String(NODE_COUNT) + ";"
                "const nodeSel=document.getElementById('node');"
                "for(let i=0;i<nodeCount;i++){let o=document.createElement('option');o.value=i;o.text='Node '+(i+1);nodeSel.appendChild(o);} "
                "function hex(n){return n.toString(16).padStart(2,'0');}"
                "function fetchState(){fetch('/api/state').then(r=>r.json()).then(s=>{"
                "document.getElementById('effect').value=s.effect;});}"
                "function applyColor(){"
                "const idx=nodeSel.value;const c=document.getElementById('color').value;"
                "const r=parseInt(c.substr(1,2),16);const g=parseInt(c.substr(3,2),16);const b=parseInt(c.substr(5,2),16);"
                "fetch(`/api/node?node=${idx}&r=${r}&g=${g}&b=${b}&on=1`,{method:'POST'}).then(fetchState);}"
                "function setOff(){const idx=nodeSel.value;fetch(`/api/node?node=${idx}&on=0`,{method:'POST'}).then(fetchState);}"
                "function applyEffect(){const e=document.getElementById('effect').value;fetch(`/api/effect?effect=${e}`,{method:'POST'}).then(fetchState);}"
                "function saveProfile(){const n=document.getElementById('profile').value;fetch(`/api/profile/save?name=${encodeURIComponent(n)}`,{method:'POST'});} "
                "function loadProfile(){const n=document.getElementById('profile').value;fetch(`/api/profile/load?name=${encodeURIComponent(n)}`,{method:'POST'}).then(fetchState);} "
                "setInterval(fetchState,3000);fetchState();"
                "</script></body></html>";
  server.send(200, "text/html", html);
}

static void setupWebRoutes() {
  server.on("/setup", HTTP_GET, handleSetupPage);
  server.on("/setup", HTTP_POST, handleSetupPost);
  server.on("/api/state", HTTP_GET, handleState);
  server.on("/api/node", HTTP_POST, handleNodeUpdate);
  server.on("/api/effect", HTTP_POST, handleEffectUpdate);
  server.on("/api/profile/save", HTTP_POST, handleProfileSave);
  server.on("/api/profile/load", HTTP_POST, handleProfileLoad);
  server.on("/api/profiles", HTTP_GET, handleProfilesList);
  server.on("/", HTTP_GET, handleControlPage);
}

static bool connectWiFi() {
  String ssid = prefs.getString(PREF_WIFI_SSID, "");
  String pass = prefs.getString(PREF_WIFI_PASS, "");
  if (ssid.length() == 0) {
    return false;
  }
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid.c_str(), pass.c_str());
  unsigned long start = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - start < 12000) {
    delay(300);
  }
  return WiFi.status() == WL_CONNECTED;
}

static void onFauxmoEvent(unsigned char deviceId, const char *deviceName, bool state, unsigned char value) {
  (void)deviceName;

  // deviceId entspricht dem Index in fauxmo.addDevice()
  if (deviceId >= NODE_COUNT) {
    return;
  }

  if (!state) {
    nodes[deviceId].on = false;
    applyNodeState(deviceId);
    return;
  }

  // Alexa liefert value (0..255) als Helligkeit
  uint8_t brightness = value;
  // Beispiel: Rot mit variabler Helligkeit
  nodes[deviceId].r = brightness;
  nodes[deviceId].g = 0;
  nodes[deviceId].b = 0;
  nodes[deviceId].on = true;
  applyNodeState(deviceId);
}

void setup() {
  Serial.begin(115200);

  Wire.begin(I2C_SDA, I2C_SCL, I2C_SPEED);

  prefs.begin(PREFS_NAMESPACE, false);
  randomSeed(esp_random());

  for (size_t i = 0; i < NODE_COUNT; ++i) {
    nodes[i] = {255, 0, 0, true};
  }

  if (!connectWiFi()) {
    apMode = true;
    WiFi.mode(WIFI_AP);
    WiFi.softAP(SETUP_SSID, SETUP_PASS);
    Serial.print("AP aktiv: ");
    Serial.println(SETUP_SSID);
  } else {
    Serial.print("Verbunden, IP: ");
    Serial.println(WiFi.localIP());
  }

  setupWebRoutes();
  server.begin();

  fauxmo.createServer(true);
  fauxmo.setPort(80);
  fauxmo.enable(true);

  for (size_t i = 0; i < NODE_COUNT; ++i) {
    String deviceName = String("RGB Node ") + (i + 1);
    fauxmo.addDevice(deviceName.c_str());
  }

  fauxmo.onSetState(onFauxmoEvent);

  // Beispiel: Adressvergabe (nur ein unzugewiesener ATmega gleichzeitig einschalten!)
  // assignAddress(0x09);
  // assignAddress(0x0A);
  // assignAddress(0x0B);
}

void loop() {
  fauxmo.handle();
  server.handleClient();
  tickEffects();
}
