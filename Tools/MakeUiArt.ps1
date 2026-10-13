# Собирает из рамки уровня (ArtSource\UI\level_button_frame.png: дерево, железные
# уголки с заклёпками) две растягиваемые детали интерфейса:
#   button_wide.png  - широкая кнопка (для "Играть", "Настройки", "Назад" и т.д.)
#   panel_board.png  - большая доска (подложка окон настроек, паузы, победы)
# Запуск (из корня проекта):
#   powershell -ExecutionPolicy Bypass -File Tools\MakeUiArt.ps1
#
# Как собирается: из рамки вырезаются четыре угловых уголка и по одному
# "ровному" куску каждой стороны (между уголком и центральным ромбом) и
# центральная заливка (тёмный пергамент). Куски растягиваются под нужный
# размер: уголки остаются как нарисованы, стороны и центр тянутся. Так в
# игре кнопка любой ширины выглядит как рамка уровня, а не как растянутая
# картинка. Размеры уголков в готовой картинке (CornerPx) должны совпадать с
# границами 9-slice в ArtImportSettings.
#
# Сообщения скрипта на английском (консоль Windows неверно показывает русские буквы).

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$src  = Join-Path $root 'ArtSource\UI\level_button_frame.png'
$out  = Join-Path $root 'Assets\_Project\Art\UI'
if (-not (Test-Path $src)) { throw "Frame not found: $src (run Tools\ProcessArt.ps1 first)" }

$frame = [System.Drawing.Bitmap]::FromFile($src)

# Source rectangles (pixels in the 1254x1254 original of the frame art).
$corner = 255
$cTL = New-Object System.Drawing.Rectangle 36, 40, $corner, $corner
$cTR = New-Object System.Drawing.Rectangle 963, 40, $corner, $corner
$cBL = New-Object System.Drawing.Rectangle 36, 960, $corner, $corner
$cBR = New-Object System.Drawing.Rectangle 963, 960, $corner, $corner
# plain stretches of each side, between a corner plate and the centre diamond
$eTop    = New-Object System.Drawing.Rectangle 305, 40, 100, $corner
$eBottom = New-Object System.Drawing.Rectangle 305, 960, 100, $corner
$eLeft   = New-Object System.Drawing.Rectangle 36, 300, $corner, 92
$eRight  = New-Object System.Drawing.Rectangle 963, 300, $corner, 92
# centre fill (dark parchment)
$centre  = New-Object System.Drawing.Rectangle 340, 340, 580, 580

function Compose($name, $wSrc, $hSrc, $scale) {
    $w = [int]($wSrc * $scale); $h = [int]($hSrc * $scale); $c = [int]($corner * $scale)
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $unit = [System.Drawing.GraphicsUnit]::Pixel
    function Draw($destX, $destY, $destW, $destH, $srcRect) {
        $dest = New-Object System.Drawing.Rectangle ([int]$destX), ([int]$destY), ([int]$destW), ([int]$destH)
        $g.DrawImage($frame, $dest, $srcRect.X, $srcRect.Y, $srcRect.Width, $srcRect.Height, $unit)
    }
    # order: centre, sides, then corners on top
    Draw ($c - 4) ($c - 4) ($w - 2 * $c + 8) ($h - 2 * $c + 8) $centre
    Draw $c 0 ($w - 2 * $c) $c $eTop
    Draw $c ($h - $c) ($w - 2 * $c) $c $eBottom
    Draw 0 $c $c ($h - 2 * $c) $eLeft
    Draw ($w - $c) $c $c ($h - 2 * $c) $eRight
    Draw 0 0 $c $c $cTL
    Draw ($w - $c) 0 $c $c $cTR
    Draw 0 ($h - $c) $c $c $cBL
    Draw ($w - $c) ($h - $c) $c $c $cBR
    $g.Dispose()
    $path = Join-Path $out $name
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host ("{0}: {1}x{2}, corner {3}px" -f $name, $w, $h, $c)
}

# Button 4:1 (menu buttons are 320x80); panel for windows.
Compose 'button_wide.png' 2200 560 0.3
Compose 'panel_board.png' 3000 1900 0.3

$frame.Dispose()
Write-Host 'Done.'
