# Рисует героя, гоблинов, двери и блоки башни (см. Tools/SpriteArt.cs).
# Запуск:  powershell -ExecutionPolicy Bypass -File Tools\MakeSpriteArt.ps1 [-ArtRoot <папка>]
# По умолчанию заменяет картинки в Assets/_Project/Art/{Characters,Doors,Towers};
# Unity переимпортирует их сам (масштаб подбирается по высоте картинки).
param(
    [string]$ArtRoot = (Join-Path $PSScriptRoot '..\Assets\_Project\Art')
)

$root = [System.IO.Path]::GetFullPath($ArtRoot)
Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $PSScriptRoot 'SpriteArt.cs')
[SpriteArt]::RenderAll((Join-Path $root 'Characters'), (Join-Path $root 'Doors'), (Join-Path $root 'Towers'))
