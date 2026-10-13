# Рисует уличные фоны: 4 фона для уровней с башнями и фон главного меню
# (см. Tools/SceneryArt.cs).
# Запуск:  powershell -ExecutionPolicy Bypass -File Tools\MakeSceneryArt.ps1 [-BackgroundsDir <папка>] [-UiDir <папка>]
# По умолчанию заменяет bg_1..4 в Assets/_Project/Art/Backgrounds и
# menu_background.png в Assets/_Project/Art/UI; Unity переимпортирует их сам.
param(
    [string]$BackgroundsDir = (Join-Path $PSScriptRoot '..\Assets\_Project\Art\Backgrounds'),
    [string]$UiDir = (Join-Path $PSScriptRoot '..\Assets\_Project\Art\UI')
)

Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $PSScriptRoot 'SceneryArt.cs')
[SceneryArt]::RenderAll([System.IO.Path]::GetFullPath($BackgroundsDir), [System.IO.Path]::GetFullPath($UiDir))
