from PIL import Image
import struct

def convert_to_rgb565_raw(input_path, output_path, size=(64, 64)):
    # Open the image, convert it to RGB, and resize it
    img = Image.open(input_path).convert('RGB')
    img = img.resize(size)
    
    with open(output_path, 'wb') as f:
        for y in range(img.height):
            for x in range(img.width):
                r, g, b = img.getpixel((x, y))
                
                # Convert 24-bit RGB to 16-bit RGB565
                rgb565 = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3)
                
                # Save it as Little-endian (2 bytes)
                f.write(struct.pack('<H', rgb565))
                
    print(f"Saved as: {output_path} (Size: {img.width * img.height * 2} bytes)")

# Запуск
convert_to_rgb565_raw("image.jpg", "icon.raw")
