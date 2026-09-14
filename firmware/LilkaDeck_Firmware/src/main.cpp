#include <Arduino.h>
#include "USB.h"
#include "USBHIDKeyboard.h"

USBHIDKeyboard Keyboard;

void setup() {
    // 1. Ініціалізуємо віртуальну клавіатуру (HID)
    Keyboard.begin();
    
    // 2. Запускаємо шину USB
    USB.begin();
    
    // 3. Запускаємо COM-порт. 
    // Завдяки ARDUINO_USB_CDC_ON_BOOT=1, це піде прямо через USB-кабель.
    Serial.begin(115200);
}

void loop() {
    // Відправляємо тестове повідомлення кожні 2 секунди
    Serial.println("Lilka Stream Deck is online: COM + HID enabled!");
    delay(2000);
}
