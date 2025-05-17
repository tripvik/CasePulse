#include <M5Unified.h>
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <freertos/stream_buffer.h>
#include <freertos/task.h>

// audio parameters
#define SAMPLE_RATE      24000
#define SAMPLE_BITS      16
#define CHANNELS         false
#define MTU_SIZE         512
#define BUFFER_SIZE      5
static constexpr size_t CHUNK_SAMPLES = 2500;
static constexpr size_t BYTES_PER_SAMPLE = sizeof(int16_t);
static constexpr size_t CHUNK_SIZE_BYTES = CHUNK_SAMPLES * BYTES_PER_SAMPLE;

// Buffer sized to hold multiple audio chunks
static constexpr size_t STREAM_BUFFER_SIZE = CHUNK_SIZE_BYTES * BUFFER_SIZE; // Space for 12 chunks
static constexpr size_t TRIGGER_LEVEL = 500; // Wake receiver when at least 1 chunk is available

// BLE UUIDs (replace with your own for production)
#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_UUID "beb5483e-36e1-4688-b7f5-ea07361b26a8"

// Connection state flag
static bool clientConnected = false;

// FreeRTOS objects
static StreamBufferHandle_t audioStreamBuffer;
BLECharacteristic* pAudioChar;

// Stats for monitoring
static uint32_t totalChunks = 0;
static uint32_t droppedBytes = 0;
static uint32_t bufferHighWatermark = 0;

static bool readyToReceive = false;
static unsigned long connectionTime = 0;
static constexpr unsigned long RECORDING_DELAY_MS = 3500; 

// Modify the BLE Server Callbacks to reset the ready state
class ServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
        clientConnected = true;
        readyToReceive = false;  // Not ready to receive immediately
        connectionTime = millis(); // Record the connection time
        
        Serial.println("Client connected - preparing audio stream...");
        // Clear the stream buffer when a new client connects
        xStreamBufferReset(audioStreamBuffer);
        // Reset stats on new connection
        totalChunks = 0;
        droppedBytes = 0;
        bufferHighWatermark = 0;
    }
    
    void onDisconnect(BLEServer* pServer) {
        clientConnected = false;
        readyToReceive = false;
        Serial.println("Client disconnected - stopping audio streaming");
        // Restart advertising so new clients can connect
        BLEDevice::startAdvertising();
    }
};


//----------------------------------------------------------------------
// Task: recordTask
//   - Blocks on M5.Mic.record() only when a client is connected
//   - Writes audio data directly to the stream buffer
//----------------------------------------------------------------------
// Modify recordTask to check the ready state
void recordTask(void* pv) {
  // Single recording buffer - no need for multiple buffers now
  int16_t* recordBuffer = (int16_t*)malloc(CHUNK_SIZE_BYTES);
  if (recordBuffer == nullptr) {
    Serial.println("Failed to allocate record buffer");
    return;
  }
  
  while (true) {
    // Check if client is connected but not yet ready to receive
    if (clientConnected && !readyToReceive) {
      // Check if delay period has elapsed
      if (millis() - connectionTime >= RECORDING_DELAY_MS) {
        readyToReceive = true;
        Serial.println("Starting audio recording now");
      } else {
        // Still in delay period, sleep briefly and check again
        vTaskDelay(pdMS_TO_TICKS(100));
        continue;
      }
    }
    
    // Only record when connected AND ready to receive
    if (clientConnected && readyToReceive) {
      if (M5.Mic.record(recordBuffer, CHUNK_SAMPLES, SAMPLE_RATE, CHANNELS)) {
        totalChunks++;
        
        // Write the data in chunks of TRIGGER_LEVEL bytes
        size_t totalBytesWritten = 0;
        size_t bytesRemaining = CHUNK_SIZE_BYTES;
        uint8_t* byteBuffer = (uint8_t*)recordBuffer;
        
        while (bytesRemaining > 0) {
          // Determine size of this chunk
          size_t chunkSize = (bytesRemaining > TRIGGER_LEVEL) ? TRIGGER_LEVEL : bytesRemaining;
          
          // Write chunk to the stream buffer
          size_t bytesWritten = xStreamBufferSend(
                                  audioStreamBuffer, 
                                  byteBuffer + totalBytesWritten,
                                  chunkSize, 
                                  pdMS_TO_TICKS(50)); // Allow up to 50ms to write
          
          totalBytesWritten += bytesWritten;
          bytesRemaining -= bytesWritten;
          
          if (bytesWritten < chunkSize) {
            droppedBytes += (chunkSize - bytesWritten);
            Serial.printf("Stream buffer full! Dropped %u bytes\n", 
                         (chunkSize - bytesWritten));
            
            // If we couldn't write the whole chunk, no point trying more
            break;
          }
        }
        
        // Track high watermark of stream buffer usage
        size_t bytesAvailable = xStreamBufferBytesAvailable(audioStreamBuffer);
        if (bytesAvailable > bufferHighWatermark) {
          bufferHighWatermark = bytesAvailable;
          Serial.printf("New buffer high watermark: %u/%u bytes\n", 
                       bufferHighWatermark, STREAM_BUFFER_SIZE);
        }
      }
    } else {
      // When no client is connected or not ready, just wait and check again
      vTaskDelay(pdMS_TO_TICKS(100));
    }
  }
}

// Also modify sendTask to check the ready state
void sendTask(void* pv) {
  // Buffer to hold data received from the stream buffer
  uint8_t* txBuffer = (uint8_t*)malloc(TRIGGER_LEVEL);
  if (txBuffer == nullptr) {
    Serial.println("Failed to allocate TX buffer");
    return;
  }
  
  while (true) {
    if (clientConnected && readyToReceive) {
      // Wait for TRIGGER_LEVEL bytes of data in the stream buffer
      size_t bytesReceived = xStreamBufferReceive(
                              audioStreamBuffer,
                              txBuffer,
                              TRIGGER_LEVEL,
                              pdMS_TO_TICKS(100));
      
      if (bytesReceived > 0) {
        // Send the entire chunk immediately
        pAudioChar->setValue(txBuffer, bytesReceived);
        pAudioChar->notify();
        
        // Small yield to let BLE stack work
        M5.delay(4);
      }
    } else {
      vTaskDelay(pdMS_TO_TICKS(100));
    }
  }
}

void setup() {
  Serial.begin(115200);
  M5.begin();
  M5.Speaker.end();

  // Create stream buffer for audio data
  audioStreamBuffer = xStreamBufferCreate(
                       STREAM_BUFFER_SIZE,   // Total buffer size in bytes
                       TRIGGER_LEVEL+TRIGGER_LEVEL);       // Minimum bytes before receiver is unblocked
  
  if (audioStreamBuffer == NULL) {
    Serial.println("Failed to create stream buffer");
    while (1) delay(100);
  }

  // init Mic
  if (!M5.Mic.begin()) {
    Serial.println("Mic init failed");
    while (1) delay(100);
  }

  // set up BLE
  BLEDevice::init("ESP32_Audio");
  BLEServer* srv = BLEDevice::createServer();
  
  // Add connection callback handler
  srv->setCallbacks(new ServerCallbacks());
  
  BLEService* svc = srv->createService(SERVICE_UUID);
  pAudioChar = svc->createCharacteristic(CHARACTERISTIC_UUID, BLECharacteristic::PROPERTY_NOTIFY);
  // Add descriptor for CCCD (Client Characteristic Configuration Descriptor)
// This is required for notifications to work properly on Android
BLE2902* p2902 = new BLE2902();
p2902->setNotifications(true);
pAudioChar->addDescriptor(p2902);

// Add user-friendly description (helps with debugging in BLE scanner apps)
BLEDescriptor* pDesc = new BLEDescriptor(BLEUUID((uint16_t)0x2901));
pDesc->setValue("Audio Stream");
pAudioChar->addDescriptor(pDesc);

// Start the service
svc->start();

// Set up advertising with parameters optimized for Android
BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
pAdvertising->addServiceUUID(SERVICE_UUID);
pAdvertising->setScanResponse(false);  // Disable scan response for Android
//pAdvertising->setMinPreferred(0x06);  // Helps with iPhone connection issues
//pAdvertising->setMaxPreferred(0x12);  // Recommended for Android

// Start advertising
BLEDevice::startAdvertising();
//BLEDevice::setMTU(MTU_SIZE);

  Serial.println("BLE audio device ready - waiting for connection...");

  // create and pin tasks to different cores with larger stack sizes
  xTaskCreatePinnedToCore(recordTask, "recordTask", 4096, nullptr, 5, nullptr, 0);
  xTaskCreatePinnedToCore(sendTask,   "sendTask",   4096, nullptr, 5, nullptr, 1);
}

void loop() {
  static unsigned long lastReport = 0;
  
  // Update display based on connection state
  M5.update();
  
  // Check and update readyToReceive status if needed
  if (clientConnected && !readyToReceive) {
    if (millis() - connectionTime >= RECORDING_DELAY_MS) {
      readyToReceive = true;
      Serial.println("Ready to transmit audio data");
    }
  }
  
  if (millis() - lastReport > 5000) { // Report every 5 seconds
    lastReport = millis();
    
    if (clientConnected) {
      if (readyToReceive) {
        float dropPercentage = (totalChunks > 0) ? 
                              ((float)droppedBytes*100.0f/((float)totalChunks*CHUNK_SIZE_BYTES)) : 0;
        
        Serial.printf("Audio stats: %u chunks, %.1f%% data dropped, buffer high: %u/%u bytes\n", 
                     totalChunks, dropPercentage, bufferHighWatermark, STREAM_BUFFER_SIZE);
      } else {
        unsigned long remaining = RECORDING_DELAY_MS - (millis() - connectionTime);
        Serial.printf("Client connected, waiting %u ms before starting audio...\n", remaining);
      }
    } else {
      Serial.println("Waiting for BLE client connection...");
    }
    
    Serial.printf("Free heap: %u bytes\n", ESP.getFreeHeap());
  }
  
  M5.delay(100);
}