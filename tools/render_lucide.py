"""Rasterize the bundled Lucide SVGs for WPF using local Edge/Chrome."""

from pathlib import Path
from subprocess import run
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ICONS = ROOT / 'Assets' / 'Lucide'
OUTPUT = ICONS / 'png'
OUTPUT.mkdir(exist_ok=True)
NAMES = [
    'play', 'pause', 'skip-back', 'skip-forward', 'rotate-ccw', 'rotate-cw',
    'volume-2', 'volume-x', 'keyboard', 'list-music', 'sliders-horizontal',
    'x', 'disc-3', 'minus', 'plus', 'chevron-left', 'chevron-right', 'monitor', 'search', 'settings-2',
]
SIZE = 96
ICON_SIZE = 72
COLUMNS = 4
ROWS = (len(NAMES) + COLUMNS - 1) // COLUMNS

cells = []
for name in NAMES:
    svg = (ICONS / f'{name}.svg').read_text(encoding='utf-8')
    svg = svg.replace('width="24"', f'width="{ICON_SIZE}"')
    svg = svg.replace('height="24"', f'height="{ICON_SIZE}"')
    svg = svg.replace('currentColor', '#ffffff')
    cells.append(f'<div class="cell">{svg}</div>')

html = ROOT / 'artifacts' / 'lucide-raster.html'
html.parent.mkdir(exist_ok=True)
html.write_text(
    '<!doctype html><html><head><style>'
    '*{box-sizing:border-box}html,body{margin:0;background:#000}'
    f'body{{width:{COLUMNS*SIZE}px;display:grid;grid-template-columns:repeat({COLUMNS},{SIZE}px)}}'
    f'.cell{{width:{SIZE}px;height:{SIZE}px;display:flex;align-items:center;justify-content:center}}'
    '</style></head><body>' + ''.join(cells) + '</body></html>', encoding='utf-8')

browser = next((p for p in [
    Path(r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'),
    Path(r'C:\Program Files\Google\Chrome\Application\chrome.exe'),
] if p.exists()), None)
if browser is None:
    raise RuntimeError('Edge/Chrome não encontrado para renderizar os SVGs do Lucide.')

screenshot = ROOT / 'artifacts' / 'lucide-raster.png'
result = run([
    str(browser), '--headless=new', '--disable-gpu', '--no-first-run',
    '--hide-scrollbars', '--force-device-scale-factor=1',
    f'--window-size={COLUMNS*SIZE},{ROWS*SIZE}',
    f'--screenshot={screenshot}', html.as_uri(),
], capture_output=True, text=True, timeout=60)
if result.returncode != 0:
    raise RuntimeError(result.stderr or f'Browser exit {result.returncode}')

sheet = Image.open(screenshot).convert('RGB')
if sheet.width < COLUMNS*SIZE or sheet.height < ROWS*SIZE:
    raise RuntimeError(f'Screenshot incompleto: {sheet.size}')

for index, name in enumerate(NAMES):
    x, y = index % COLUMNS * SIZE, index // COLUMNS * SIZE
    crop = sheet.crop((x, y, x+SIZE, y+SIZE))
    pixels = crop.load()
    result_icon = Image.new('RGBA', (SIZE, SIZE), (255, 255, 255, 0))
    dst = result_icon.load()
    for py in range(SIZE):
        for px in range(SIZE):
            alpha = max(pixels[px, py])
            dst[px, py] = (255, 255, 255, alpha)
    result_icon.save(OUTPUT / f'{name}.png')
    print(name)

app_icon = Image.new('RGBA', (256, 256), (0, 0, 0, 0))
from PIL import ImageDraw
ImageDraw.Draw(app_icon).rounded_rectangle((5, 5, 250, 250), radius=49, fill='#078BEA')
play = Image.open(OUTPUT / 'play.png').convert('RGBA').resize((210, 210), Image.Resampling.LANCZOS)
app_icon.alpha_composite(play, (23, 23))
app_icon.save(ROOT / 'Assets' / 'MediaControl.ico', format='ICO', sizes=[(16, 16), (32, 32), (48, 48), (256, 256)])
