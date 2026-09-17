#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"
#include "config/BoardConfig.h"
#include "input/InputManager.h"
#include "display/DisplayManager.h"
#include "storage/StorageManager.h"

USBHIDKeyboard Keyboard;
InputManager inputManager(Keyboard);
DisplayManager displayManager;
StorageManager storageManager;

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
    
    // Allow USB stack to stabilize and host OS to enumerate the HID device
    delay(200);

    Serial0.println("[SYS] Starting InputManager...");
    inputManager.begin();
    
    Serial0.println("[SYS] Starting DisplayManager...");
    // Initializes TFT_eSPI and its configured SPI instance.
    displayManager.begin(); 

    Serial0.println("[SYS] Starting StorageManager...");
    // Explicit Dependency Injection: Share the initialized SPI bus with SD Card
    SPIClass& sharedSpiBus = displayManager.getSharedSpiBus();

    if (storageManager.begin(sharedSpiBus, BoardConfig::PIN_SD_CS)) {
        Serial0.println("[SYS] Attempting to load /icon.raw...");
        
        if (storageManager.readFileToBuffer("/icon.raw", iconBuffer, ICON_BUFFER_SIZE)) {
            uint16_t* rgb565Data = reinterpret_cast<uint16_t*>(iconBuffer);

            // Array of all positions for grid testing
            IconPosition positions[] = {
                IconPosition::LeftUp, IconPosition::LeftLeft, 
                IconPosition::LeftRight, IconPosition::LeftDown,
                IconPosition::RightUp, IconPosition::RightLeft, 
                IconPosition::RightRight, IconPosition::RightDown
            };

            // Draw an icon in each cell
            for (IconPosition pos : positions) {
                displayManager.drawIcon(pos, rgb565Data);
            }
            
            Serial0.println("[SYS] All 8 icons rendered successfully!");
        }
    }

    Serial0.println("[SYS] Lilka Stream Deck: Ready.");
}

void loop() {
    // Process input non-blockingly to maintain HID responsiveness
    inputManager.update();
    
    // Yield CPU to prevent Watchdog Timer (WDT) triggers
    delay(1);
}
