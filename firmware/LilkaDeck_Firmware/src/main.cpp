#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"

USBHIDKeyboard Keyboard;
ConfigManager configManager;

// Dependency Injection: Pass the populated configuration to the InputManager
InputManager inputManager(Keyboard, configManager);
DisplayManager displayManager;
StorageManager storageManager;

constexpr size_t ICON_WIDTH = 64;
constexpr size_t ICON_HEIGHT = 64;
constexpr size_t ICON_BUFFER_SIZE = ICON_WIDTH * ICON_HEIGHT * 2;
uint8_t iconBuffer[ICON_BUFFER_SIZE] __attribute__((aligned(4)));

void setup() {
    Serial0.begin(115200);
    Serial0.println("\n--- LILKA BOOT SEQUENCE START ---");

    // Secure SPI bus state before initialization sequence
    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial0.println("[SYS] Starting USB HID...");
    Keyboard.begin();
    USB.begin();
    delay(200); // Allow OS to enumerate the USB device

    Serial0.println("[SYS] Starting DisplayManager...");
    displayManager.begin(); 

    Serial0.println("[SYS] Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial0.println("[SYS] Reading configuration payload...");
        
        String jsonConfig = storageManager.readTextFile("/config.json");
        
        if (jsonConfig.length() > 0 && configManager.loadConfig(jsonConfig)) {
            Serial0.println("[SYS] Configuration parsed successfully. Rendering UI layout...");

            // Iterate through logical positions and render allocated assets
            for (const auto& pair : configManager.getButtons()) {
                IconPosition pos = pair.first;
                String iconPath = pair.second.iconPath;
                
                if (storageManager.readFileToBuffer(iconPath.c_str(), iconBuffer, ICON_BUFFER_SIZE)) {
                    uint16_t* rgb565Data = reinterpret_cast<uint16_t*>(iconBuffer);
                    displayManager.drawIcon(pos, rgb565Data);
                }
            }
            Serial0.println("[SYS] UI layout initialization complete.");
        } else {
            Serial0.println("[ERR] Failed to load or evaluate config.json payload.");
        }
    }

    // Initialize inputs after the configuration map is populated
    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    // Non-blocking polling
    inputManager.update();
    
    // Relinquish CPU slightly to prevent WDT resets
    delay(1);
}
