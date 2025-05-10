#include <Arduino.h>
#include <M5Unified.h>
#include "Startup/startup.h"
#include "BluetoothSerial.h"
#include <queue>

// Define a queue to hold chunks of data to be sent
std::queue<std::vector<uint8_t>> btSendQueue;

// statics
static constexpr const size_t record_size      = 10000;
static constexpr const size_t record_samplerate = 24000;
static int16_t *rec_data;

// globals
BluetoothSerial SerialBT;
bool btConnected = false;

// Arduino Methods
void setup() {
  // Initialize M5
  auto cfg = M5.config();
  M5.begin(cfg);
  setupLogging();

  // Allocate recording buffer
  M5.Log(ESP_LOG_VERBOSE, "Allocating memory for rec_data...");
  rec_data = (int16_t*)malloc(record_size * sizeof(int16_t));
  if (!rec_data) {
    M5.Log(ESP_LOG_ERROR, "Failed to allocate rec_data!");
    while (1) delay(1000);
  }

  // Mic setup
  auto miccfg = M5.Mic.config();
  miccfg.magnification = 32;      // 0–32
  miccfg.noise_filter_level = (miccfg.noise_filter_level + 8) & 0xFF;
  M5.Mic.config(miccfg);
  M5.Mic.begin();

  // Start Bluetooth Classic SPP
  if (!SerialBT.begin("M5_Serial")) {
    M5.Log(ESP_LOG_ERROR, "BluetoothSerial failed to start");
    while (1) delay(1000);
  }
  M5.Log(ESP_LOG_INFO, "Bluetooth classic SPP ready. Pair with device name: M5_Serial");
  //also print the BT address
  
}

void loop() {
  // Check connection
  btConnected = SerialBT.hasClient();

  if (btConnected) {
    // Record into rec_data[]
    if (M5.Mic.record(rec_data, record_size, record_samplerate, false)) {
      // Break data into smaller chunks and enqueue them
      for (size_t i = 0; i < record_size; i += 500) {
        size_t chunkSize = min<size_t>(500, record_size - i);
        std::vector<uint8_t> chunk((uint8_t*)rec_data + i, (uint8_t*)rec_data + i + chunkSize);
        btSendQueue.push(chunk);
      }
    } else {
      M5.Log(ESP_LOG_ERROR, "Mic.record() failed");
    }
  }

  // Process the Bluetooth send queue
  if (!btSendQueue.empty()) {
    auto& chunk = btSendQueue.front();
    size_t written = SerialBT.write(chunk.data(), chunk.size());
    if (written == chunk.size()) {
      btSendQueue.pop(); // Remove the chunk from the queue if fully sent
    } else {
      M5.Log(ESP_LOG_WARN, "Partial write occurred, retrying...");
    }
  }

  // Allow other tasks to run
  M5.delay(5);
}
