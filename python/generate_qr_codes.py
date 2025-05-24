import qrcode
from PIL import Image, ImageDraw, ImageFont
import os
import sys

# ======== Configuration ========
BASE_URLS = {
    "dev": "https://dev.provision41.com/dr/dumplog?id=",
    "prod": "https://provision41.com/dr/dumplog?id="
}
FONT_PATH = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
FONT_SIZE_HEADER = 36
FONT_SIZE_FOOTER = 18
TOP_PADDING = 60
BOTTOM_PADDING = 80
SIDE_PADDING = 40
START_ID = 1000
COUNT = 100

# ======== Input Argument ========
if len(sys.argv) != 2 or sys.argv[1] not in BASE_URLS:
    print("Usage: python generate_qr_codes.py [dev|prod]")
    sys.exit(1)

env = sys.argv[1]
base_url = BASE_URLS[env]
output_dir = f"qr_codes/{env}"
os.makedirs(output_dir, exist_ok=True)

# ======== Fonts ========
font_header = ImageFont.truetype(FONT_PATH, FONT_SIZE_HEADER)
font_footer = ImageFont.truetype(FONT_PATH, FONT_SIZE_FOOTER)

# ======== QR Code Generation Loop ========
for i in range(START_ID, START_ID + COUNT):
    serial = str(i)
    truck_text = f"Truck # {serial}"
    footer_line1 = "Provision41.com Disaster Relief"
    footer_line2 = "Barrineau & Garza Elite Contracting"
    url = f"{base_url}{serial}"

    # Generate QR code
    qr_img = qrcode.make(url).convert("RGB")
    qr_w, qr_h = qr_img.size

    # Measure text widths
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

    img_width = max(qr_w, max_text_width) + SIDE_PADDING * 2
    img_height = qr_h + TOP_PADDING + BOTTOM_PADDING
    final_img = Image.new("RGB", (img_width, img_height), "white")
    draw = ImageDraw.Draw(final_img)

    # Header text
    header_x = (img_width - (bbox_header[2] - bbox_header[0])) // 2
    draw.text((header_x, 10), truck_text, fill="black", font=font_header)

    # QR Code
    qr_x = (img_width - qr_w) // 2
    qr_y = TOP_PADDING
    final_img.paste(qr_img, (qr_x, qr_y))

    # Footer text
    footer1_x = (img_width - (bbox_footer1[2] - bbox_footer1[0])) // 2
    footer2_x = (img_width - (bbox_footer2[2] - bbox_footer2[0])) // 2
    draw.text((footer1_x, qr_y + qr_h + 10), footer_line1, fill="black", font=font_footer)
    draw.text((footer2_x, qr_y + qr_h + 10 + FONT_SIZE_FOOTER + 4), footer_line2, fill="black", font=font_footer)

    # Save file
    file_path = os.path.join(output_dir, f"qr_{env}_{serial}.png")
    final_img.save(file_path)
    print(f"✅ Saved {file_path}")
