#include <Arduino.h>
#include <Wire.h>
#include <EEPROM.h>

// ===== I2C =====
constexpr uint8_t DEFAULT_ADDRESS = 0x08; // Nur temporär, wird nach der Zuweisung ignoriert
constexpr uint8_t EEPROM_ADDR_LOCATION = 0; // 1 Byte
constexpr uint8_t EEPROM_COLOR_LOCATION = 4; // 3 Bytes (R,G,B)

// ===== I2C Protokoll =====
constexpr uint8_t CMD_SET_RGB = 0x10;       // 3 Bytes: R, G, B
constexpr uint8_t CMD_SET_OFF = 0x11;       // LED aus
constexpr uint8_t RESP_STATUS = 0x7E;       // Antwort-Byte bei I2C-Request
constexpr uint8_t CMD_ASSIGN_ADDRESS = 0xA0; // Payload: 1 Byte neue Adresse (General Call)

// ===== RGB Pins (PWM) =====
constexpr uint8_t PIN_R = 3;
constexpr uint8_t PIN_G = 5;
constexpr uint8_t PIN_B = 6;

uint8_t currentAddress = DEFAULT_ADDRESS;

static void saveAddress(uint8_t addr) {
  EEPROM.update(EEPROM_ADDR_LOCATION, addr);
  currentAddress = addr;
}

static void loadAddress() {
  uint8_t stored = EEPROM.read(EEPROM_ADDR_LOCATION);
  if (stored >= 0x08 && stored <= 0x77) {
    currentAddress = stored;
  } else {
    currentAddress = DEFAULT_ADDRESS;
  }
}

static void saveColor(uint8_t r, uint8_t g, uint8_t b) {
  EEPROM.update(EEPROM_COLOR_LOCATION, r);
  EEPROM.update(EEPROM_COLOR_LOCATION + 1, g);
  EEPROM.update(EEPROM_COLOR_LOCATION + 2, b);
}

static void loadColor(uint8_t &r, uint8_t &g, uint8_t &b) {
  r = EEPROM.read(EEPROM_COLOR_LOCATION);
  g = EEPROM.read(EEPROM_COLOR_LOCATION + 1);
  b = EEPROM.read(EEPROM_COLOR_LOCATION + 2);
}

static void applyRgb(uint8_t r, uint8_t g, uint8_t b) {
  analogWrite(PIN_R, r);
  analogWrite(PIN_G, g);
  analogWrite(PIN_B, b);
}

static void onReceive(int count) {
  if (count <= 0) {
    return;
  }

  uint8_t cmd = Wire.read();

  if (cmd == CMD_SET_RGB && count >= 4) {
    uint8_t r = Wire.read();
    uint8_t g = Wire.read();
    uint8_t b = Wire.read();
    applyRgb(r, g, b);
    saveColor(r, g, b);
    return;
  }

  if (cmd == CMD_SET_OFF) {
    applyRgb(0, 0, 0);
    return;
  }

  if (cmd == CMD_ASSIGN_ADDRESS && count >= 2) {
    uint8_t newAddr = Wire.read();
    if (newAddr >= 0x08 && newAddr <= 0x77) {
      saveAddress(newAddr);
      Wire.begin(currentAddress);
    }
  }
}

static void onRequest() {
  // Master fragt Status ab: 1 Byte senden
  Wire.write(RESP_STATUS);
}

void setup() {
  pinMode(PIN_R, OUTPUT);
  pinMode(PIN_G, OUTPUT);
  pinMode(PIN_B, OUTPUT);

  loadAddress();

  Wire.begin(currentAddress);
#if defined(TWAR)
  // General Call aktivieren, damit der ESP32 eine Adresse vergeben kann
  TWAR = (currentAddress << 1) | 0x01;
#endif
  Wire.onReceive(onReceive);
  Wire.onRequest(onRequest);

  // Startzustand: aus
  uint8_t r = 0;
  uint8_t g = 0;
  uint8_t b = 0;
  loadColor(r, g, b);
  applyRgb(r, g, b);
}

void loop() {
  // Keine Logik nötig
}
