#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"

USBHIDKeyboard Keyboard;
InputManager inputManager(Keyboard);
DisplayManager displayManager;
StorageManager storageManager;
ConfigManager configManager;

constexpr size_t ICON_WIDTH = 64;
constexpr size_t ICON_HEIGHT = 64;
constexpr size_t ICON_BUFFER_SIZE = ICON_WIDTH * ICON_HEIGHT * 2;
uint8_t iconBuffer[ICON_BUFFER_SIZE] __attribute__((aligned(4)));

void setup() {
    Serial0.begin(115200);
    Serial0.println("\n--- LILKA BOOT SEQUENCE START ---");

    // 1. Hardware Protection: Secure SPI bus state before any initialization
    // Keep SD deselected before the SPI bus is used by other peripherals.
    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial0.println("[SYS] Starting USB...");
    Keyboard.begin();
    USB.begin();
    delay(200); // Allow USB stack to stabilize and host OS to enumerate the HID device

    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();
    
    Serial0.println("[SYS] Starting DisplayManager...");
    displayManager.begin(); // Initializes TFT_eSPI and its configured SPI instance.

    Serial0.println("[SYS] Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus(); // Explicit Dependency Injection: Share the initialized SPI bus with SD Card

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial0.println("[SYS] Reading config.json...");
        
        // 1. Read the config file as text
        String jsonConfig = storageManager.readTextFile("/config.json");
        
        if (jsonConfig.length() > 0 && configManager.loadConfig(jsonConfig)) {
            Serial0.println("[SYS] Config parsed successfully. Drawing icons...");

            // 2. Go through all the buttons in the configuration
            for (const auto& pair : configManager.getButtons()) {
                IconPosition pos = pair.first;
                String iconPath = pair.second.iconPath;
                
                // 3. Load the corresponding .raw file
                if (storageManager.readFileToBuffer(iconPath.c_str(), iconBuffer, ICON_BUFFER_SIZE)) {
                    uint16_t* rgb565Data = reinterpret_cast<uint16_t*>(iconBuffer);
                    
                    // 4. Drawing on the screen
                    displayManager.drawIcon(pos, rgb565Data);
                    Serial0.printf("[SYS] Rendered %s\n", iconPath.c_str());
                } else {
                    Serial0.printf("[ERR] Missing icon: %s\n", iconPath.c_str());
                }
            }
            Serial0.println("[SYS] All layout rendered successfully!");
        } else {
            Serial0.println("[ERR] Failed to load or parse config.json");
        }
    }

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    inputManager.update();
    delay(1);
}
