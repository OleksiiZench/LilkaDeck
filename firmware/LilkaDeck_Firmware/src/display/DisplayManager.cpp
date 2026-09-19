#include "display/DisplayManager.h"
#include "config/BoardConfig.h"
#include <Arduino.h>

DisplayManager::DisplayManager() : _tft() {
}

void DisplayManager::begin() {
    pinMode(BoardConfig::PIN_POWER_ENABLE, OUTPUT);
    digitalWrite(BoardConfig::PIN_POWER_ENABLE, HIGH);
    
    // 1. Жорстко гасимо підсвітку до будь-яких операцій
    const uint8_t PIN_BLK = 46;
    pinMode(PIN_BLK, OUTPUT);
    digitalWrite(PIN_BLK, LOW); 
    
    delay(50); // Даємо час на стабілізацію живлення матриці

    // Ініціалізація контролера
    _tft.init();

    // 2. ХАК ДЛЯ TFT_eSPI: Обхід програмної обрізки
    // Скидаємо ротацію в нуль, щоб отримати доступ до сирих координат
    _tft.setRotation(0); 
    
    // Відкриваємо вікно на ВСЮ фізичну пам'ять чипа (240x320), ігноруючи розмір панелі Лілки
    _tft.startWrite();
    _tft.setWindow(0, 0, 240, 320); 
    // Заливаємо всі 76800 пікселів чорним кольором безперервним блоком
    _tft.pushBlock(TFT_BLACK, 240 * 320); 
    _tft.endWrite();

    // 3. Тепер безпечно виставляємо нашу ландшафтну орієнтацію
    _tft.setRotation(3);
    _tft.invertDisplay(true);
    
    // 4. Робимо фіктивний клір у правильній ротації (про всяк випадок для внутрішніх змінних бібліотеки)
    _tft.fillScreen(TFT_BLACK);
    
    // Даємо мікросекунду матриці на оновлення кадру, перш ніж вмикати світло
    delay(20); 
    
    // 5. Вмикаємо екран. Тепер пам'ять ідеально чиста від краю до краю.
    digitalWrite(PIN_BLK, HIGH);
    
    Serial0.println("[TFT] Display initialized with full hardware GRAM wipe.");
}

void DisplayManager::clear() {
    _tft.startWrite();
    _tft.drawPixel(0, 0, TFT_BLACK);
    _tft.endWrite();

    _tft.fillScreen(TFT_BLACK);
}

SPIClass& DisplayManager::getSharedSpiBus() {
    return _tft.getSPIinstance();
}

void DisplayManager::drawIcon(IconPosition pos, uint16_t* imageBuffer) {
    int32_t x, y;
    getIconCoordinates(pos, x, y);

    _tft.startWrite();
    _tft.drawPixel(0, 0, TFT_BLACK);
    _tft.endWrite();

    _tft.setSwapBytes(true); 
    _tft.pushImage(x, y, 64, 64, imageBuffer);
}

void DisplayManager::showBootScreen()
{
    clear();

    // Set text alignment to Middle Center
    _tft.setTextDatum(MC_DATUM);

    // Draw main title in white
    _tft.setTextColor(TFT_WHITE, TFT_BLACK);
    _tft.setTextSize(3); // Multiplier for the default font
    
    // Width is 280, Height is 240 (in rotation 3)
    // Draw slightly above the absolute vertical center
    _tft.drawString("LILKA DECK", _tft.width() / 2, _tft.height() / 2 - 15);

    // Draw subtitle in grey
    _tft.setTextColor(TFT_DARKGREY, TFT_BLACK);
    _tft.setTextSize(1);
    
    // Draw slightly below the vertical center
    _tft.drawString("Loading configuration...", _tft.width() / 2, _tft.height() / 2 + 25);
}

void DisplayManager::getIconCoordinates(IconPosition pos, int32_t& x, int32_t& y) {
    // Base Y-coordinates for the three rows
    const int32_t ROW_UP = 18;
    const int32_t ROW_MID = 88;
    const int32_t ROW_DOWN = 158;

    switch (pos) {
        // --- LEFT SIDE ---
        case IconPosition::LeftLeft:  x = 4;   y = ROW_MID;  break;
        case IconPosition::LeftUp:    x = 37;  y = ROW_UP;   break;
        case IconPosition::LeftDown:  x = 37;  y = ROW_DOWN; break;
        case IconPosition::LeftRight: x = 70;  y = ROW_MID;  break;

        // --- RIGHT SIDE ---
        case IconPosition::RightLeft:  x = 146; y = ROW_MID;  break; 
        case IconPosition::RightUp:    x = 179; y = ROW_UP;   break;
        case IconPosition::RightDown:  x = 179; y = ROW_DOWN; break;
        case IconPosition::RightRight: x = 212; y = ROW_MID;  break;
    }
}
