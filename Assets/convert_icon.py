from PIL import Image
from pathlib import Path
folder = Path(r"Z:\Kevin Laptop Files\D__Codex Projects\visualizer\Windows\OpenMD\Assets")
image = Image.open(folder / "OpenMD.png").convert("RGBA")
image.save(folder / "OpenMD.ico", format="ICO", sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
print("Created multi-resolution ICO:", image.size)
