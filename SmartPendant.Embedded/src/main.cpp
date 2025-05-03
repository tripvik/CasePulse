#include <Arduino.h>
#include <M5Unified.h>
#include "Startup\startup.h"
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

// References
//=================
// BLE Sample->https://github.com/mo-thunderz/Esp32BlePart1/blob/main/Arduino/BLE_server/BLE_server.ino
// Now Integrated with ESP32-Arduino - https://github.com/espressif/arduino-esp32/blob/master/libraries/BLE/examples/Server/Server.ino

// defines
//================
#define SERVICE_UUID "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"

// statics
//================
static constexpr const size_t record_size = 10000;
static constexpr const size_t record_samplerate = 16000;
static uint8_t *rec_data;

// globals
//==================
// BLE variables
//==================
BLEServer *pServer = NULL;
BLECharacteristic *pCharacteristic = NULL;
BLEDescriptor *pDescr;
BLE2902 *pBLE2902;
bool deviceConnected = false;
bool oldDeviceConnected = false;
uint32_t value = 0;

// function Declaration
//===================

// Class Declarations
//===================
class MyServerCallbacks : public BLEServerCallbacks
{
  void onConnect(BLEServer *pServer)
  {
    deviceConnected = true;
    M5.Log(ESP_LOG_INFO, "Client connected");
    // get the client device name
  };

  void onDisconnect(BLEServer *pServer)
  {
    M5.Log(ESP_LOG_INFO, "Client diconnected");
    deviceConnected = false;
  }
};

// Arduino Methods
//==================
void setup()
{
  // put your setup code here, to run once:
  auto cfg = M5.config();
  M5.begin(cfg);
  setupLogging();

  // Allocating memory for recording data
  //========================================
  M5.Log(ESP_LOG_VERBOSE, "Allocating memory for rec_data...");
  rec_data = (uint8_t *)malloc(record_size);
  if (rec_data == nullptr)
  {
    M5.Log(ESP_LOG_ERROR, "Failed to allocate memory for rec_data!");
    while (true)
      ;
  }

  // Mic setup
  //========================================
  auto miccfg = M5.Mic.config();
  //miccfg.noise_filter_level = (miccfg.noise_filter_level - 8) & 255;
  //M5.Log(ESP_LOG_VERBOSE, "Mic magnification: %d", miccfg.magnification);
  //miccfg.magnification = 32; // 0-32
  M5.Mic.config(miccfg);
  M5.Mic.begin();

  // BLE setup
  //=================
  BLEDevice::init("ESP32");
  // Create the BLE Server
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());

  // Create the BLE Service
  BLEService *pService = pServer->createService(SERVICE_UUID);

  // Create a BLE Characteristic
  pCharacteristic = pService->createCharacteristic(
      CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_NOTIFY);

  // Create a BLE Descriptor

  pDescr = new BLEDescriptor((uint16_t)0x2901);
  pDescr->setValue("A very interesting variable");
  pCharacteristic->addDescriptor(pDescr);

  pBLE2902 = new BLE2902();
  pBLE2902->setNotifications(true);
  pCharacteristic->addDescriptor(pBLE2902);

  // Start the service
  pService->start();

  // Start advertising
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
 pAdvertising->setMinPreferred(0x0); // set value to 0x00 to not advertise this parameter
                        // request larger MTU, or 247 if your client supports it

  BLEDevice::startAdvertising();
  M5.Log(ESP_LOG_INFO, "Waiting a client connection to notify...");
}

void loop()
{
  if (deviceConnected) {
    if (M5.Mic.record(rec_data, record_size, record_samplerate, false)) {
        // Send chunks of 500 bytes due to BLE limitations
        for (size_t i = 0; i < record_size; i += 500) {
            size_t chunk_size = std::min<size_t>(500, record_size - i);
            pCharacteristic->setValue(rec_data + i, chunk_size);
            pCharacteristic->notify();
            M5.delay(5); // Allow Bluetooth stack to process events
        }
    } else {
        M5.Log(ESP_LOG_ERROR, "Record failed");
    }
} else if (!deviceConnected && oldDeviceConnected) {
    // Handle disconnection
    M5.delay(500); // Allow Bluetooth stack to reset
    pServer->startAdvertising();
    M5.Log(ESP_LOG_INFO, "Start advertising");
    oldDeviceConnected = deviceConnected;
} else {
    // Waiting for client connection
    M5.delay(500);
}

if (deviceConnected && !oldDeviceConnected) {
    // Handle new connection
    oldDeviceConnected = deviceConnected;
}
}

/// for ESP-IDF
#if !defined(ARDUINO) && defined(ESP_PLATFORM)
extern "C"
{
  void loopTask(void *)
  {
    setup();
    for (;;)
    {
      loop();
    }
    vTaskDelete(NULL);
  }

  void app_main()
  {
    xTaskCreatePinnedToCore(loopTask, "loopTask", 8192, NULL, 1, NULL, 1);
  }
}
#endif
