import qrcode
from PIL import Image, ImageDraw, ImageFont
import os

# Output folder
output_dir = "qr_codes"
os.makedirs(output_dir, exist_ok=True)

# Font config
font_path = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"  # macOS built-in bold font
font_size_header = 36  # ⬆ Larger header
font_size_footer = 18  # ⬇ Smaller footer
font_header = ImageFont.truetype(font_path, font_size_header)
font_footer = ImageFont.truetype(font_path, font_size_footer)

# Settings
base_url = "https://provision41.com/dr/dumplog?id="
top_padding = 60
bottom_padding = 80
side_padding = 40

for i in range(1000, 1100):
    serial = str(i)
    truck_text = f"Truck # {serial}"
    footer_line1 = "Provision41.com Disaster Relief"
    footer_line2 = "Barrineau & Garza Elite Contracting"
    url = f"{base_url}{serial}"

    # Generate QR code
    qr_img = qrcode.make(url).convert("RGB")
    qr_w, qr_h = qr_img.size

    # Dummy draw to measure text
    dummy = Image.new("RGB", (1, 1))
    draw_dummy = ImageDraw.Draw(dummy)
    bbox_header = draw_dummy.textbbox((0, 0), truck_text, font=font_header)
    bbox_footer1 = draw_dummy.textbbox((0, 0), footer_line1, font=font_footer)
    bbox_footer2 = draw_dummy.textbbox((0, 0), footer_line2, font=font_footer)

    max_text_width = max(
        bbox_header[2] - bbox_header[0],
        bbox_footer1[2] - bbox_footer1[0],
        bbox_footer2[2] - bbox_footer2[0]
    )

    img_width = max(qr_w, max_text_width) + side_padding * 2
    img_height = qr_h + top_padding + bottom_padding
    final_img = Image.new("RGB", (img_width, img_height), "white")
    draw = ImageDraw.Draw(final_img)

    # Header text
    header_x = (img_width - (bbox_header[2] - bbox_header[0])) // 2
    draw.text((header_x, 10), truck_text, fill="black", font=font_header)

    # QR Code
    qr_x = (img_width - qr_w) // 2
    qr_y = top_padding
    final_img.paste(qr_img, (qr_x, qr_y))

    # Footer lines
    footer1_x = (img_width - (bbox_footer1[2] - bbox_footer1[0])) // 2
    footer2_x = (img_width - (bbox_footer2[2] - bbox_footer2[0])) // 2
    draw.text((footer1_x, qr_y + qr_h + 10), footer_line1, fill="black", font=font_footer)
    draw.text((footer2_x, qr_y + qr_h + 10 + font_size_footer + 4), footer_line2, fill="black", font=font_footer)

    # Save image
    file_path = os.path.join(output_dir, f"qr_{serial}.png")
    final_img.save(file_path)
    print(f"✅ Saved {file_path}")
