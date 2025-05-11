#include <Arduino.h>
#include <M5Unified.h>
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include "Startup/startup.h"
#include "esp_task_wdt.h"

// BLE UUIDs
#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"

// Audio settings
static constexpr size_t record_size = 5000;  // Reduced from 5000
static constexpr size_t record_samplerate = 24000;

// Queue settings - Increased for better buffering
#define QUEUE_LENGTH 50
#define NUM_BUFFERS  5

// Buffer pool
static int16_t rec_buffers[NUM_BUFFERS][record_size];
static int buffer_index = 0;

// Queue and task handles
static QueueHandle_t payloadQueue = nullptr;
static TaskHandle_t bleTaskHandle = nullptr;
static TaskHandle_t micTaskHandle = nullptr;

// Connection management
static uint32_t connection_timestamp = 0;
static const uint32_t CLIENT_INIT_DELAY_MS = 2000; // Wait 2 seconds after connection before sending
static bool readyToSend = false;

// BLE globals
BLEServer* pServer = nullptr;
BLECharacteristic* pCharacteristic = nullptr;
bool deviceConnected = false;
bool oldDeviceConnected = false;

// Semaphore for buffer synchronization
static SemaphoreHandle_t bufferSemaphore = nullptr;

// Status indicators
static size_t queue_high_watermark = 0;
static uint32_t dropped_frames = 0;
static uint32_t frames_sent = 0;
static uint32_t last_status_time = 0;

// Forward declarations
void bleProcessingTask(void*);
void micRecordingTask(void*);

class MyServerCallbacks : public BLEServerCallbacks {
  void onConnect(BLEServer*) override {
    deviceConnected = true;
    readyToSend = false;  // Reset sending flag
    connection_timestamp = millis();  // Record connection time
    M5.Log(ESP_LOG_VERBOSE, "BLE client connected - waiting for client to initialize");
    
    // Clear any pending data in the queue
    xQueueReset(payloadQueue);
  }
  
  void onDisconnect(BLEServer*) override {
    deviceConnected = false;
    readyToSend = false;
    M5.Log(ESP_LOG_INFO, "BLE client disconnected");
  }
};

// Create custom descriptor for client to read MTU
class MTUDescriptorCallback : public BLEDescriptorCallbacks {
  void onRead(BLEDescriptor* pDescriptor) {
    uint16_t mtu = BLEDevice::getMTU();
    String mtuStr = String(mtu);
    pDescriptor->setValue(mtuStr.c_str());
    M5.Log(ESP_LOG_INFO, "Client read MTU descriptor: %d", mtu);
  }
};

void setup() {
  auto cfg = M5.config();
  M5.begin(cfg);
  setupLogging();

  // Create the queue (stores pointers to int16_t arrays)
  payloadQueue = xQueueCreate(QUEUE_LENGTH, sizeof(int16_t*));
  if (!payloadQueue) {
    M5.Log(ESP_LOG_ERROR, "Failed to create payloadQueue!");
    while (1) delay(1000);
  }

  // Create buffer semaphore
  bufferSemaphore = xSemaphoreCreateMutex();
  if (!bufferSemaphore) {
    M5.Log(ESP_LOG_ERROR, "Failed to create buffer semaphore!");
    while (1) delay(1000);
  }

  // Mic setup
  auto miccfg = M5.Mic.config();
  M5.Mic.config(miccfg);
  M5.Mic.begin();

  // BLE setup with higher MTU
  BLEDevice::init("ESP32-Audio");
  BLEDevice::setMTU(512);  // Try to negotiate a higher MTU
  
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());
  auto pService = pServer->createService(SERVICE_UUID);

  pCharacteristic = pService->createCharacteristic(
    CHARACTERISTIC_UUID,
    BLECharacteristic::PROPERTY_NOTIFY
  );

  // Add descriptors
  auto pDesc = new BLEDescriptor((uint16_t)0x2901);
  pDesc->setValue("Audio stream");
  pCharacteristic->addDescriptor(pDesc);

  // Add custom MTU descriptor to help client
  auto pMtuDesc = new BLEDescriptor(BLEUUID((uint16_t)0x2902));
  pMtuDesc->setCallbacks(new MTUDescriptorCallback());
  pMtuDesc->setValue("0");
  pCharacteristic->addDescriptor(pMtuDesc);

  auto p2902 = new BLE2902();
  p2902->setNotifications(true);
  pCharacteristic->addDescriptor(p2902);

  pService->start();
  auto pAdvert = BLEDevice::getAdvertising();
  pAdvert->addServiceUUID(SERVICE_UUID);
  pAdvert->setScanResponse(true);  // Enable scan response
  pAdvert->setMinPreferred(0x06);  // Helps with iPhone connections
  pAdvert->setMinPreferred(0x12);
  BLEDevice::startAdvertising();
  M5.Log(ESP_LOG_INFO, "Waiting for BLE client...");
  esp_task_wdt_delete(xTaskGetIdleTaskHandleForCPU(0));

  // Create microphone recording task on core 1
  xTaskCreatePinnedToCore(
    micRecordingTask,
    "micRec",
    4096,
    nullptr,
    2,  // Lower priority than BLE task
    &micTaskHandle,
    1  // Core 1 (same as Arduino loop)
  );

  // Disabling due to watch dog timer resets:

  // Create BLE processing task on core 0
  xTaskCreatePinnedToCore(
    bleProcessingTask,
    "bleProc",
    8192,
    nullptr,
    configMAX_PRIORITIES - 1,  // Highest priority
    &bleTaskHandle,
    0  // Core 0
  );
  
  // Print initial queue information
  M5.Log(ESP_LOG_INFO, "Queue size: %d, Buffer size: %d samples", QUEUE_LENGTH, record_size);
  last_status_time = millis();
}

void loop() {
  M5.update();  // Update buttons, etc.
  
  // Check if we should start sending data after connection delay
  if (deviceConnected && !readyToSend && millis() - connection_timestamp > CLIENT_INIT_DELAY_MS) {
    readyToSend = true;
    M5.Log(ESP_LOG_INFO, "Starting data transmission after init delay");
  }
  
  // Update status periodically
  if (millis() - last_status_time > 5000) {
    UBaseType_t queueItems = uxQueueMessagesWaiting(payloadQueue);
    M5.Log(ESP_LOG_VERBOSE, "Status: Connected=%d Ready=%d, Queue=%d/%d (max=%d), Sent=%u, Dropped=%u", 
           deviceConnected ? 1 : 0, readyToSend ? 1 : 0, queueItems, QUEUE_LENGTH, queue_high_watermark, 
           frames_sent, dropped_frames);
    
    last_status_time = millis();
  }
  
  // Handle reconnection
  if (!deviceConnected && oldDeviceConnected) {
    delay(500);
    BLEDevice::startAdvertising();
    M5.Log(ESP_LOG_INFO, "Re-start advertising");
    oldDeviceConnected = deviceConnected;
  } else if (deviceConnected && !oldDeviceConnected) {
    oldDeviceConnected = deviceConnected;
  }
  
  delay(10);  // Short delay to yield to other tasks
}

void micRecordingTask(void* /*param*/) {
  TickType_t xLastWakeTime = xTaskGetTickCount();
  int last_buffer_index = -1;
  
  while (true) {
    // Only record if device is connected and ready to send
    if (!deviceConnected || !readyToSend) {
      vTaskDelayUntil(&xLastWakeTime, pdMS_TO_TICKS(100));
      continue;
    }
    
    // Get current buffer
    if (xSemaphoreTake(bufferSemaphore, portMAX_DELAY) == pdTRUE) {
      int16_t* current_buffer = rec_buffers[buffer_index];
      last_buffer_index = buffer_index;
      buffer_index = (buffer_index + 1) % NUM_BUFFERS;
      xSemaphoreGive(bufferSemaphore);
      
      // Record audio to buffer
      if (M5.Mic.record(current_buffer, record_size, record_samplerate, false)) {
        // Check queue availability before trying to send
        UBaseType_t queueItems = uxQueueMessagesWaiting(payloadQueue);
        if (queueItems < QUEUE_LENGTH) {
          if (xQueueSend(payloadQueue, &current_buffer, 0) != pdTRUE) {
            dropped_frames++;
          } else {
            // Update high watermark
            if (queueItems + 1 > queue_high_watermark) {
              queue_high_watermark = queueItems + 1;
            }
          }
        } else {
          dropped_frames++;
        }
      } else {
        M5.Log(ESP_LOG_ERROR, "Mic.record failed");
        vTaskDelay(pdMS_TO_TICKS(10));  // Short delay on error
      }
    }
    
    // Small yield to allow other tasks to run - maintain consistent timing
    vTaskDelayUntil(&xLastWakeTime, pdMS_TO_TICKS(10));
  }
}

// BLE processing task
// This task handles sending audio data over BLE
// It runs on core 0 and has the highest priority
// It processes audio data in chunks to avoid exceeding MTU size
void bleProcessingTask(void* /*param*/) {
  const size_t CHUNK_SIZE = 500;  // Reduce chunk size
  const TickType_t NOTIFY_DELAY = pdMS_TO_TICKS(20);  // Increase delay between notifications
  
  while (true) {
    if (!deviceConnected || !readyToSend) {
      vTaskDelay(pdMS_TO_TICKS(100));
      continue;
    }
    
    int16_t* dataPtr = nullptr;
    if (xQueueReceive(payloadQueue, &dataPtr, pdMS_TO_TICKS(10)) == pdTRUE) {
      // Process the audio data in chunks
      for (size_t idx = 0; idx < record_size; idx += CHUNK_SIZE) {
        // Check if we're still connected and ready
        if (!deviceConnected || !readyToSend) break;
        
        size_t chunk = min<size_t>(CHUNK_SIZE, record_size - idx);
        
        // Get current MTU size
        uint16_t mtu = BLEDevice::getMTU();
        size_t max_bytes = mtu - 3; // Account for ATT header (3 bytes)
        size_t bytes_to_send = chunk * sizeof(int16_t);
        
        // Make sure we don't exceed MTU
        if (bytes_to_send > max_bytes) {
          chunk = max_bytes / sizeof(int16_t);
          bytes_to_send = chunk * sizeof(int16_t);
        }
        
        // Add error handling for notification
        try {
          pCharacteristic->setValue(
            (uint8_t*)(dataPtr + idx),
            bytes_to_send
          );
          
          // Check if we can send a notification
          if (deviceConnected && readyToSend) {
            pCharacteristic->notify(); // Exit the loop if disconnected
          }
        } catch (...) {
          M5.Log(ESP_LOG_ERROR, "Exception during notification");
          vTaskDelay(NOTIFY_DELAY * 5);
          continue;
        }
        
        // Increased delay between notifications
        vTaskDelay(NOTIFY_DELAY);
      }
      
      frames_sent++;
    } else {
      // No data in queue, short sleep to prevent tight loop
      vTaskDelay(pdMS_TO_TICKS(5));
    }
  }
}