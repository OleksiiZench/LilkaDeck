#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "config/ConfigManager.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"

USBHIDKeyboard keyboard;
ConfigManager configManager;
DisplayManager displayManager;
StorageManager storageManager;
InputManager inputManager(keyboard, configManager, displayManager);


constexpr size_t ICON_WIDTH = 64;
constexpr size_t ICON_HEIGHT = 64;
constexpr size_t ICON_BUFFER_SIZE = ICON_WIDTH * ICON_HEIGHT * 2;
uint8_t iconBuffer[ICON_BUFFER_SIZE] __attribute__((aligned(4)));

void setup() {
    // Immediately disable the display backlight to suppress hardware boot artifacts
    pinMode(46, OUTPUT);
    digitalWrite(46, LOW);

    Serial0.begin(115200);
    Serial0.println("\n--- LILKA BOOT SEQUENCE START ---");

    // Secure the SPI bus state before initializing shared peripherals
    pinMode(BoardConfig::PIN_SD_CS, OUTPUT);
    digitalWrite(BoardConfig::PIN_SD_CS, HIGH);

    Serial0.println("[SYS] Starting USB HID...");
    keyboard.begin();
    USB.begin();
    
    // Provide sufficient delay for the host OS to enumerate the USB HID device
    delay(200); 

    Serial0.println("[SYS] Starting DisplayManager...");
    displayManager.begin();

    displayManager.showBootScreen();
    delay(1000);

    Serial0.println("[SYS] Starting StorageManager...");
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial0.println("[SYS] Reading configuration payload...");
        
        String jsonConfig = storageManager.readTextFile("/config.json");
        
        if (jsonConfig.length() > 0 && configManager.loadConfig(jsonConfig)) {
            Serial0.println("[SYS] Configuration parsed successfully. Rendering UI layout...");

            displayManager.clear();

            // Iterate through mapped logical positions and render the assigned assets
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

    // Initialize the input polling matrix after the configuration is fully loaded
    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    // Process input states non-blockingly
    inputManager.update();
    
    // Relinquish CPU time to the RTOS scheduler to prevent WDT resets
    delay(1);
}
