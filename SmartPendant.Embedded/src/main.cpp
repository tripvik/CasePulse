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
static constexpr const size_t record_size = 185;
static constexpr const size_t record_samplerate = 8000;
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
  M5.Log(ESP_LOG_INFO, "Allocating memory for rec_data...");
  rec_data = (uint8_t *)malloc(record_size);
  if (rec_data == nullptr)
  {
    M5.Log(ESP_LOG_ERROR, "Failed to allocate memory for rec_data!");
    while (true)
      ;
  }

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
  BLEDevice::startAdvertising();
  M5.Log(ESP_LOG_INFO, "Waiting a client connection to notify...");
}



void loop()
{
  if (deviceConnected)
  {
    if (M5.Mic.record(rec_data, record_size, record_samplerate, false))
    {
      pCharacteristic->setValue(rec_data, record_size);
      pCharacteristic->notify();
      // M5.Log(ESP_LOG_INFO, "Recorded and sent data over BLE");
    }
    else
    {
      M5.Log(ESP_LOG_ERROR, "Record failed");
    }
  }
  // disconnecting
  if (!deviceConnected && oldDeviceConnected)
  {
    M5.delay(500);               // give the bluetooth stack the chance to get things ready
    pServer->startAdvertising(); // restart advertising
    M5.Log(ESP_LOG_INFO, "start advertising");
    oldDeviceConnected = deviceConnected;
  }
  // connecting
  if (deviceConnected && !oldDeviceConnected)
  {
    // do stuff here on connecting
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
