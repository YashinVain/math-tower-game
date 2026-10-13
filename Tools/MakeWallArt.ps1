# Рисует фоны-стены для уровней с дверями (см. Tools/WallArt.cs).
# Запуск:  powershell -ExecutionPolicy Bypass -File Tools\MakeWallArt.ps1 [-OutDir <папка>]
# По умолчанию картинки кладутся в Assets/_Project/Art/Backgrounds (заменяют
# bg_castle_1..4); после этого Unity сам их переимпортирует.
param(
    [string]$OutDir = (Join-Path $PSScriptRoot '..\Assets\_Project\Art\Backgrounds')
)

Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $PSScriptRoot 'WallArt.cs')
$resolved = [System.IO.Path]::GetFullPath($OutDir)
[WallArt]::RenderAll($resolved)
Write-Output ("Feet line from bottom (fraction of image height): {0:0.0000}" -f [WallArt]::FeetLineFromBottom)
