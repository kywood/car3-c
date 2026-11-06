
from datetime import datetime
from pathlib import Path
import subprocess


SCREENSHOT_DIR = (
    Path(__file__).resolve().parent.parent
    / "reports"
    / "screenshots"
)


def capture_screen() -> Path:
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)

    filename = f"unity_{datetime.now():%Y%m%d_%H%M%S}.png"
    output_path = SCREENSHOT_DIR / filename

    windows_path = subprocess.check_output(
        ["wslpath", "-w", str(output_path)],
        text=True,
    ).strip()

    # PowerShell 문자열 안의 작은따옴표 이스케이프
    safe_path = windows_path.replace("'", "''")

    ps_script = rf"""
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds

$bmp = New-Object System.Drawing.Bitmap(
    $bounds.Width,
    $bounds.Height
)

$graphics = [System.Drawing.Graphics]::FromImage($bmp)

try {{
    $graphics.CopyFromScreen(
        $bounds.Location,
        [System.Drawing.Point]::Empty,
        $bounds.Size
    )

    $bmp.Save(
        '{safe_path}',
        [System.Drawing.Imaging.ImageFormat]::Png
    )
}}
finally {{
    $graphics.Dispose()
    $bmp.Dispose()
}}
"""

    result = subprocess.run(
        ["powershell.exe", "-NoProfile", "-Command", ps_script],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=30,
    )

    if result.returncode != 0:
        error = result.stderr.decode("cp949", errors="replace")
        raise RuntimeError(f"Windows 화면 캡처 실패: {error}")

    if not output_path.is_file():
        raise RuntimeError("스크린샷 파일이 생성되지 않았습니다.")

    return output_path
