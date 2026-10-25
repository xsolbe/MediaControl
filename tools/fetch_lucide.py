"""Fetch the original SVGs used by the WPF interface from Lucide."""

from pathlib import Path
from urllib.request import urlopen

ROOT = Path(__file__).resolve().parents[1] / 'Assets' / 'Lucide'
ROOT.mkdir(parents=True, exist_ok=True)
ICONS = [
    'play', 'pause', 'skip-back', 'skip-forward', 'rotate-ccw', 'rotate-cw',
    'volume-2', 'volume-x', 'keyboard', 'list-music', 'sliders-horizontal',
    'x', 'disc-3', 'minus', 'plus', 'chevron-left', 'chevron-right', 'monitor', 'search', 'settings-2',
]
BASE = 'https://raw.githubusercontent.com/lucide-icons/lucide/main/'

for name in ICONS:
    target = ROOT / f'{name}.svg'
    if not target.exists():
        with urlopen(BASE + f'icons/{name}.svg', timeout=20) as source:
            target.write_bytes(source.read())
    print(name)

license_file = ROOT / 'LUCIDE-LICENSE.txt'
if not license_file.exists():
    with urlopen(BASE + 'LICENSE', timeout=20) as source:
        license_file.write_bytes(source.read())
