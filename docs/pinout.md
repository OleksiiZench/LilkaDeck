# Lilka Hardware Pinout (ESP32-S3)

Цей документ описує прив'язку GPIO мікроконтролера ESP32-S3 до периферії консолі Лілка.

## 🎮 Кнопки (Buttons)
Використовується підтяжка `INPUT_PULLUP`. Активний рівень — `LOW` (замикання на GND).

| Кнопка | GPIO | Опис |
| :--- | :--- | :--- |
| **Up** | `38` | D-Pad Вгору |
| **Down** | `41` | D-Pad Вниз |
| **Left** | `39` | D-Pad Вліво |
| **Right** | `40` | D-Pad Вправо |
| **A** | `5` | Action A |
| **B** | `6` | Action B |
| **C** | `10` | Action C |
| **D** | `9` | Action D |
| **Select** | `0` | Control Select (Режим прошивання) |
| **Start** | `4` | Control Start |

## 📺 Дисплей (TFT SPI)
Дисплей ST7789 (240x280). Ділить шину SPI з SD-картою. Пін RST не підключений (NC).

| Сигнал | GPIO | Опис |
| :--- | :--- | :--- |
| **MOSI** | `17` | Master Out Slave In (Спільний) |
| **SCK** | `18` | Serial Clock (Спільний) |
| **CS** | `7` | Chip Select (Екран) |
| **DC** | `15` | Data / Command |
| **BLK** | `46` | Підсвітка (Sleep/Backlight) |

## 💾 MicroSD (SD SPI)
Шина для читання зображень налаштувань. Ділить шину SPI з дисплеєм.

| Сигнал | GPIO | Опис |
| :--- | :--- | :--- |
| **MISO** | `8` | Master In Slave Out |
| **MOSI** | `17` | Master Out Slave In (Спільний) |
| **SCK** | `18` | Serial Clock (Спільний) |
| **CS** | `16` | Chip Select (SD) |
