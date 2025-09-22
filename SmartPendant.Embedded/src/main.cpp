#include <M5Unified.h>
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <freertos/stream_buffer.h>
#include <freertos/task.h>
#include "Startup/startup.h"
#include "resources.h"
#include <math.h>

// Color definitions for better readability
#define UI_BLACK      0x0000
#define UI_WHITE      0xFFFF
#define UI_RED        0xF800
#define UI_GREEN      0x07E0
#define UI_BLUE       0x001F
#define UI_YELLOW     0xFFE0
#define UI_DARKRED    0x8000
#define UI_DARKGREY   0x4208
#define UI_LIGHTGREY  0xBDF7

// audio parameters
#define SAMPLE_RATE      16000
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
static unsigned long lastReport = 0;
static bool readyToReceive = false;
static unsigned long connectionTime = 0;
static constexpr unsigned long RECORDING_DELAY_MS = 3500; 

// UI variables
static unsigned long lastUIUpdate = 0;
static constexpr unsigned long UI_UPDATE_INTERVAL = 50; // Update UI every 50ms for smoother animation
static float breathingPhase = 0.0;
static unsigned long lastBatteryCheck = 0;
static constexpr unsigned long BATTERY_CHECK_INTERVAL = 5000; // Check battery every 5 seconds
static int batteryPercentage = 100;
static bool lastClientConnected = false;
static bool lastReadyToReceive = false;
static bool forceFullRedraw = true;

// Task handles
static TaskHandle_t uiTaskHandle = nullptr; 

// UI Drawing Functions - Optimized for portrait mode and flicker-free updates
void drawBluetoothIcon(bool connected, bool forceRedraw = false) {
  static bool lastConnectedState = false;
  
  if (!forceRedraw && lastConnectedState == connected) {
    return; // No change, skip redraw
  }
  
  lastConnectedState = connected;
  
  int x = 15; // Moved to left side
  int y = 15;
  
  // Clear the area first
  M5.Display.fillRect(x - 5, y - 5, 25, 25, UI_BLACK);
  
  // Draw Bluetooth icon based on SVG path - simplified for small display
  uint16_t iconColor = connected ? UI_BLUE : UI_DARKGREY;
  
  // Draw the Bluetooth symbol (more accurate representation)
  // Main vertical line (thicker for better visibility)
  M5.Display.drawLine(x + 7, y + 1, x + 7, y + 17, iconColor);
  M5.Display.drawLine(x + 8, y + 1, x + 8, y + 17, iconColor);
  
  // Upper triangle/arrow
  M5.Display.drawLine(x + 7, y + 1, x + 12, y + 5, iconColor);
  M5.Display.drawLine(x + 12, y + 5, x + 7, y + 9, iconColor);
  
  // Lower triangle/arrow  
  M5.Display.drawLine(x + 7, y + 9, x + 12, y + 13, iconColor);
  M5.Display.drawLine(x + 12, y + 13, x + 7, y + 17, iconColor);
  
  // Cross lines for the characteristic Bluetooth shape
  M5.Display.drawLine(x + 4, y + 6, x + 7, y + 9, iconColor);
  M5.Display.drawLine(x + 7, y + 9, x + 4, y + 12, iconColor);
  
  // Fill some pixels to make it more solid
  M5.Display.drawPixel(x + 8, y + 4, iconColor);
  M5.Display.drawPixel(x + 9, y + 5, iconColor);
  M5.Display.drawPixel(x + 8, y + 14, iconColor);
  M5.Display.drawPixel(x + 9, y + 13, iconColor);
  
  // Add connection status indicator
  if (connected) {
    M5.Display.fillCircle(x + 16, y + 3, 2, UI_GREEN);
  } else {
    M5.Display.fillCircle(x + 16, y + 3, 2, UI_RED);
  }
}

void drawBatteryIcon(int percentage, bool forceRedraw = false) {
  static int lastPercentage = -1;
  
  if (!forceRedraw && lastPercentage == percentage) {
    return; // No change, skip redraw
  }
  
  lastPercentage = percentage;
  
  // Move to right side
  int width = 25;
  int height = 12;
  int x = M5.Display.width() - width - 35; // Leave space for percentage text
  int y = 15;
  
  // Clear the area first
  M5.Display.fillRect(x - 2, y - 2, width + 40, height + 4, UI_BLACK);
  
  // Battery outline
  M5.Display.drawRect(x, y, width, height, UI_WHITE);
  M5.Display.drawRect(x + width, y + 3, 3, height - 6, UI_WHITE);
  
  // Battery fill based on percentage
  int fillWidth = (width - 2) * percentage / 100;
  uint16_t fillColor = UI_GREEN;
  
  if (percentage < 20) {
    fillColor = UI_RED;
  } else if (percentage < 50) {
    fillColor = UI_YELLOW;
  }
  
  if (fillWidth > 0) {
    M5.Display.fillRect(x + 1, y + 1, fillWidth, height - 2, fillColor);
  }
  
  // Battery percentage text - now on the right side
  M5.Display.setTextColor(UI_WHITE);
  M5.Display.setTextSize(1);
  M5.Display.setCursor(x + width + 8, y + 3);
  M5.Display.printf("%d%%", percentage);
}

void drawRecordingIndicator() {
  int centerX = M5.Display.width() / 2;
  int centerY = M5.Display.height() / 2 + 10;
  
  // Clear only the circle area to reduce flicker
  static int lastBreathingRadius = 0;
  static bool textDrawn = false;
  
  // Reset text drawn flag when function is called for first time in this state
  static bool wasRecording = false;
  if (!wasRecording) {
    textDrawn = false;
    wasRecording = true;
  }
  
  // Calculate breathing effect
  breathingPhase += 0.10;
  if (breathingPhase > 2 * PI) {
    breathingPhase = 0;
  }
  
  // Base radius with breathing effect
  int baseRadius = 30;
  int breathingRadius = baseRadius + (int)(6 * sin(breathingPhase));
  
  // Clear previous circle if radius changed significantly
  if (abs(breathingRadius - lastBreathingRadius) > 1) {
    int clearRadius = max(lastBreathingRadius, breathingRadius) + 6;
    M5.Display.fillCircle(centerX, centerY, clearRadius, UI_BLACK);
  }
  lastBreathingRadius = breathingRadius;
  
  // Draw outer breathing circle (lighter red)
  uint16_t outerColor = M5.Display.color565(255, 100, 100); // Light red
  M5.Display.fillCircle(centerX, centerY, breathingRadius + 3, outerColor);
  
  // Draw main recording circle
  M5.Display.fillCircle(centerX, centerY, breathingRadius, UI_RED);
  
  // Static text - only draw once or when state changes
  if (!textDrawn) {
    // Clear text area
    M5.Display.fillRect(0, centerY + 45, M5.Display.width(), 60, UI_BLACK);
    
    // Add recording text below the circle
    M5.Display.setTextColor(UI_WHITE);
    M5.Display.setTextSize(1);
    M5.Display.setTextDatum(MC_DATUM);
    M5.Display.drawString("RECORDING", centerX, centerY + breathingRadius + 20);
    textDrawn = true;
  }
  
  // Add audio level indicator (simple animation) - only update bars area
  static int audioLevel = 0;
  static unsigned long lastBarUpdate = 0;
  
  if (millis() - lastBarUpdate > 100) { // Update bars every 100ms
    lastBarUpdate = millis();
    audioLevel = (audioLevel + 1) % 20;
    
    // Clear bars area
    M5.Display.fillRect(centerX - 25, centerY + 55, 50, 15, UI_BLACK);
    
    int barHeight = 3 + (audioLevel % 6);
    for (int i = 0; i < 5; i++) {
      int barX = centerX - 20 + (i * 10);
      int barY = centerY + 65;
      int currentHeight = barHeight - abs(i - 2); // Peak in middle
      if (currentHeight > 0) {
        M5.Display.fillRect(barX, barY - currentHeight, 6, currentHeight, UI_GREEN);
      }
    }
  }
}

// Function to reset recording state when switching modes
void resetRecordingTextState() {
  // This will be called from updateUI when switching from recording mode
}

void drawConnectionStatus(bool forceRedraw = false) {
  static int lastConnectionState = -1; // -1 = init, 0 = disconnected, 1 = connected, 2 = recording
  
  int currentState = 0;
  if (clientConnected && readyToReceive) {
    currentState = 2; // Recording
  } else if (clientConnected) {
    currentState = 1; // Connected but not ready
  } else {
    currentState = 0; // Disconnected
  }
  
  if (!forceRedraw && lastConnectionState == currentState) {
    return; // No change in connection state
  }
  
  lastConnectionState = currentState;
  
  int centerX = M5.Display.width() / 2;
  int centerY = M5.Display.height() / 2;
  
  // Clear the central area
  M5.Display.fillRect(0, 40, M5.Display.width(), M5.Display.height() - 80, UI_BLACK);
  
  M5.Display.setTextDatum(MC_DATUM); // Middle center
  
  if (!clientConnected) {
    // Draw app icon above the device name (moved higher up)
    int iconX = centerX - 12; // Center the 23px icon (new logo is 23x24px)
    int iconY = centerY - 65; // Moved higher up from -50 to -65
    M5.Display.drawXBitmap(iconX, iconY, epd_bitmap_CareSense, 24, 24, UI_WHITE);
    
    // Draw device name below the icon
    M5.Display.setTextColor(UI_WHITE);
    M5.Display.setTextSize(2);
    M5.Display.drawString("CarePulse", centerX, centerY - 20);
    
    // Draw connection status
    M5.Display.setTextColor(UI_LIGHTGREY);
    M5.Display.setTextSize(1);
    M5.Display.drawString("Waiting for", centerX, centerY + 10);
    M5.Display.drawString("connection...", centerX, centerY + 25);
    
  } else if (!readyToReceive) {
    // Draw app icon above the connected status (moved higher up)
    int iconX = centerX - 12; // Center the 23px icon (new logo is 23x24px)
    int iconY = centerY - 65; // Moved higher up from -50 to -65
    M5.Display.drawXBitmap(iconX, iconY, epd_bitmap_CareSense, 24, 24, UI_GREEN);
    
    // Draw connected status
    M5.Display.setTextColor(UI_GREEN);
    M5.Display.setTextSize(2);
    M5.Display.drawString("Connected", centerX, centerY - 20);
    
    // Draw preparation message
    M5.Display.setTextColor(UI_YELLOW);
    M5.Display.setTextSize(1);
    M5.Display.drawString("Preparing audio...", centerX, centerY + 10);
    
    // // Show countdown
    // unsigned long remaining = RECORDING_DELAY_MS - (millis() - connectionTime);
    // M5.Display.setTextColor(UI_WHITE);
    // M5.Display.setTextSize(2);
    // M5.Display.drawString(String(remaining / 1000 + 1) + "s", centerX, centerY + 35);
  }
}

// Helper function to reset recording text state
void resetRecordingState() {
  static bool* textDrawnPtr = nullptr;
  // This is a simple way to reset the static variable in drawRecordingIndicator
  // We'll clear the center area when switching modes
}

void updateBatteryPercentage() {
  if (millis() - lastBatteryCheck > BATTERY_CHECK_INTERVAL) {
    lastBatteryCheck = millis();
    
    // Get battery voltage and convert to percentage
    float voltage = M5.Power.getBatteryVoltage()/1000.0; // Convert to volts
    
    if (voltage > 0) {
      // Convert voltage to percentage (approximate values for LiPo battery)
      // 4.2V = 100%, 3.7V = 50%, 3.2V = 0%
      M5.Log(ESP_LOG_DEBUG, "Battery voltage: %.2fV", voltage);
      batteryPercentage = (int)((voltage - 3.2) / (4.2 - 3.2) * 100);
      //M5.Log(ESP_LOG_DEBUG, "Battery percentage: %d%%", batteryPercentage);
      batteryPercentage = constrain(batteryPercentage, 0, 100);

    } else {
      // If we can't read voltage, assume USB powered
      batteryPercentage = 100;
    }
  }
}

void updateUI() {
  // Check if we need a full redraw
  bool stateChanged = (lastClientConnected != clientConnected) || 
                     (lastReadyToReceive != readyToReceive) ||
                     forceFullRedraw;
  
  if (forceFullRedraw) {
    // Full screen clear only on first run or major state changes
    M5.Display.fillScreen(UI_BLACK);
    forceFullRedraw = false;
  }
  
  // Update battery percentage
  updateBatteryPercentage();
  
  // Draw battery icon (only if changed)
  drawBatteryIcon(batteryPercentage, stateChanged);
  
  // Draw Bluetooth status (only if changed)
  drawBluetoothIcon(clientConnected, stateChanged);
  
  // Draw main content based on state
  if (clientConnected && readyToReceive) {
    // Clear connection status area when switching to recording
    if (stateChanged && !lastReadyToReceive) {
      M5.Display.fillRect(0, 40, M5.Display.width(), M5.Display.height() - 80, UI_BLACK);
    }
    // Show recording indicator with breathing effect (always update for animation)
    drawRecordingIndicator();
  } else {
    // Clear any recording remnants when switching from recording
    if (stateChanged && lastReadyToReceive) {
      M5.Display.fillRect(0, 40, M5.Display.width(), M5.Display.height() - 80, UI_BLACK);
    }
    // Show connection status (only if state changed)
    drawConnectionStatus(stateChanged);
  }
  
  // Update state tracking
  lastClientConnected = clientConnected;
  lastReadyToReceive = readyToReceive;
}

//----------------------------------------------------------------------
// Task: uiTask
//   - Handles all UI updates and rendering
//   - Runs at lower priority than audio tasks
//----------------------------------------------------------------------
void uiTask(void* pv) {
  // Initialize UI on this task
  forceFullRedraw = true;
  
  while (true) {
    // Update UI with optimized rendering
    updateUI();
    
    // UI task delay - 50ms for smooth animation
    vTaskDelay(pdMS_TO_TICKS(50));
  }
}

// Modify the BLE Server Callbacks to reset the ready state
class ServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
        clientConnected = true;
        readyToReceive = false;  // Not ready to receive immediately
        connectionTime = millis(); // Record the connection time
        
        M5.Log(ESP_LOG_INFO ,"Client connected - preparing audio stream...");
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
        M5.Log(ESP_LOG_INFO,"Client disconnected - stopping audio streaming");
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
    M5.Log(ESP_LOG_ERROR ,"Failed to allocate record buffer");
    return;
  }
  
  while (true) {
    // Check if client is connected but not yet ready to receive
    if (clientConnected && !readyToReceive) {
      // Check if delay period has elapsed
      if (millis() - connectionTime >= RECORDING_DELAY_MS) {
        readyToReceive = true;
        M5.Log(ESP_LOG_VERBOSE ,"Starting audio recording now");
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
            M5.Log(ESP_LOG_VERBOSE ,"Stream buffer full! Dropped %u bytes\n", 
                         (chunkSize - bytesWritten));
            
            // If we couldn't write the whole chunk, no point trying more
            break;
          }
        }
        
        // Track high watermark of stream buffer usage
        size_t bytesAvailable = xStreamBufferBytesAvailable(audioStreamBuffer);
        if (bytesAvailable > bufferHighWatermark) {
          bufferHighWatermark = bytesAvailable;
          M5.Log(ESP_LOG_VERBOSE ,"New buffer high watermark: %u/%u bytes\n", 
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
    M5.Log(ESP_LOG_ERROR ,"Failed to allocate TX buffer");
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
  
  // Initialize display for UI - Portrait mode optimizations
  //M5.Display.setRotation(0); // Portrait mode
  M5.Display.setBrightness(100);
  M5.Display.fillScreen(UI_BLACK);
  M5.Display.setTextColor(UI_WHITE);
  M5.Display.setSwapBytes(true); // Fix for some color issues
  M5.Display.startWrite(); // Keep SPI bus open for faster updates
  M5.Display.fillScreen(UI_BLACK);
  M5.Display.endWrite();
  
  setupLogging();

  // Create stream buffer for audio data
  audioStreamBuffer = xStreamBufferCreate(
                       STREAM_BUFFER_SIZE,   // Total buffer size in bytes
                       TRIGGER_LEVEL+TRIGGER_LEVEL);       // Minimum bytes before receiver is unblocked
  
  if (audioStreamBuffer == NULL) {
    M5.Log(ESP_LOG_ERROR ,"Failed to create stream buffer");
    while (1) delay(100);
  }

  // init Mic
  if (!M5.Mic.begin()) {
    M5.Log(ESP_LOG_ERROR ,"Mic init failed");
    while (1) delay(100);
  }

  // set up BLE
  BLEDevice::init("CareSense"); // Device name
  BLEDevice::setMTU(MTU_SIZE);

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

  M5.Log(ESP_LOG_INFO ,"BLE audio device ready - waiting for connection...");

  // Create and pin tasks to different cores with priority hierarchy
  // Priority levels: 7 = highest (record), 5 = high (send), 3 = medium (UI)
  
  // Create UI task on core 1 with medium priority
  xTaskCreatePinnedToCore(uiTask, "uiTask", 3072, nullptr, 3, &uiTaskHandle, 1);
  
  // Create record task on core 0 with highest priority  
  xTaskCreatePinnedToCore(recordTask, "recordTask", 4096, nullptr, 7, nullptr, 0);
  
  // Create send task on core 1 with high priority
  xTaskCreatePinnedToCore(sendTask, "sendTask", 4096, nullptr, 5, nullptr, 1);
}

void loop() {
  // All functionality moved to dedicated tasks
  // Main loop is kept empty for optimal task scheduling
  vTaskDelay(pdMS_TO_TICKS(1000)); // Sleep for 1 second
}

void diagnostics() {
   // Update display based on connection state
  //M5.update();
  
  if (millis() - lastReport > 5000) { // Report every 5 seconds
    lastReport = millis();
    
    if (clientConnected) {
      if (readyToReceive) {
        float dropPercentage = (totalChunks > 0) ? 
                              ((float)droppedBytes*100.0f/((float)totalChunks*CHUNK_SIZE_BYTES)) : 0;
        
        M5.Log(ESP_LOG_VERBOSE ,"Audio stats: %u chunks, %.1f%% data dropped, buffer high: %u/%u bytes\n", 
                     totalChunks, dropPercentage, bufferHighWatermark, STREAM_BUFFER_SIZE);
      } else {
        unsigned long remaining = RECORDING_DELAY_MS - (millis() - connectionTime);
        M5.Log(ESP_LOG_VERBOSE ,"Client connected, waiting %u ms before starting audio...\n", remaining);
      }
    } else {
      M5.Log(ESP_LOG_VERBOSE ,"Waiting for BLE client connection...");
    }
    
    M5.Log(ESP_LOG_VERBOSE ,"Free heap: %u bytes\n", ESP.getFreeHeap());
  }
  
  M5.delay(100);
}