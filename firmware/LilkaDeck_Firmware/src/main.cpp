#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"
#include "core/ProfileManager.h"

USBHIDKeyboard Keyboard;
ConfigManager configManager;
DisplayManager displayManager;
StorageManager storageManager;
ProfileManager profileManager(configManager, storageManager, displayManager);
InputManager inputManager(Keyboard, configManager, displayManager, profileManager);

// --- SYNC STATE MACHINE VARIABLES ---
bool isSyncing = false;
unsigned long lastSyncTime = 0;
uint8_t syncProfileId = 0;
size_t expectedBytes = 0;
size_t receivedBytes = 0;

void handleSerialCommands() {
    // --- НОВИЙ БЛОК: ЗАХИСТ ВІД ЗАВИСАНЬ ---
    if (isSyncing && expectedBytes > 0) {
        // Якщо комп'ютер мовчить більше 3 секунд - скидаємо стан
        if (millis() - lastSyncTime > 3000) {
            Serial.println("[ERR] Sync timeout! Resetting state.");
            isSyncing = false;
            expectedBytes = 0;
            storageManager.closeFile();
        }
    }

    if (!Serial.available()) return;

    lastSyncTime = millis();

    // BINARY RECEIVE MODE
    if (isSyncing && expectedBytes > 0) {
        const size_t CHUNK_SIZE = 256;
        uint8_t buffer[CHUNK_SIZE];
        
        size_t bytesToRead = expectedBytes - receivedBytes;
        if (bytesToRead > CHUNK_SIZE) bytesToRead = CHUNK_SIZE;
        
        // Read strictly only what's currently in the hardware buffer
        size_t available = Serial.available();
        if (bytesToRead > available) bytesToRead = available;

        if (bytesToRead > 0) {
            size_t readCount = Serial.readBytes(buffer, bytesToRead);
            storageManager.writeChunk(buffer, readCount);
            receivedBytes += readCount;

            // Did we finish the file?
            if (receivedBytes >= expectedBytes) {
                storageManager.closeFile();
                expectedBytes = 0;
                Serial.println("ACK_DONE");
            }
        }
        return;
    }

    // TEXT COMMAND MODE
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.length() == 0) return;

    if (cmd.startsWith("SYNC_START:")) {
        syncProfileId = cmd.substring(11).toInt();
        isSyncing = true;
        expectedBytes = 0;
        
        String dir = "/profile_" + String(syncProfileId);
        storageManager.createDir(dir.c_str());
        Serial.println("ACK_SYNC");
    }
    else if (isSyncing && cmd.startsWith("FILE_START:")) {
        // cmd format: FILE_START:config.json:450
        int firstColon = cmd.indexOf(':');
        int secondColon = cmd.indexOf(':', firstColon + 1);
        
        String fileName = cmd.substring(firstColon + 1, secondColon);
        expectedBytes = cmd.substring(secondColon + 1).toInt();
        receivedBytes = 0;
        
        String filePath = "/profile_" + String(syncProfileId) + "/" + fileName;
        storageManager.openFileForWrite(filePath.c_str());
        
        Serial.println("ACK_FILE");
    }
    else if (isSyncing && cmd == "SYNC_END") {
        isSyncing = false;
        expectedBytes = 0;
        Serial.println("ACK_END");
        
        // Force refresh UI to show newly downloaded assets
        profileManager.loadProfile(syncProfileId);
    }
}

void setup() {
    pinMode(46, OUTPUT);
    digitalWrite(46, LOW);

    Serial.begin(115200);
    // VERY IMPORTANT: Prevent text parser from blocking the main loop
    Serial.setTimeout(50); 
    
    Serial.println("\n--- LILKA BOOT SEQUENCE START ---");

    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial.println("[SYS] Starting USB HID...");
    Keyboard.begin();
    USB.begin();
    delay(200);

    Serial.println("[SYS] Starting DisplayManager...");
    displayManager.begin();
    displayManager.showBootScreen();
    delay(1000);

    Serial.println("[SYS] Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial.println("[SYS] Booting ProfileManager...");
        profileManager.begin();
    } else {
        Serial.println("[ERR] SD Card mount failed.");
    }

    Serial.println("[SYS] Starting InputManager...");
    inputManager.begin();

    Serial.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    handleSerialCommands();
    
    // Block physical button triggers while flashing new configs to SD
    if (!isSyncing) {
        inputManager.update();
        delay(1);
    }
}
