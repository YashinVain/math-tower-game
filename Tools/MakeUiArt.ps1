# Рисует детали интерфейса (см. Tools/UiArt.cs): растягиваемые кнопку и доску,
# рамку и значки уровней (замок, медаль с галочкой) и детали слайдера.
# Запуск:  powershell -ExecutionPolicy Bypass -File Tools\MakeUiArt.ps1 [-OutDir <папка>]
# По умолчанию заменяет картинки в Assets/_Project/Art/UI; Unity
# переимпортирует их сам. Уголки растягиваемых картинок (76 px) должны
# совпадать с ArtSpecs.SlicedCornerPx.
param(
    [string]$OutDir = (Join-Path $PSScriptRoot '..\Assets\_Project\Art\UI')
)

Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $PSScriptRoot 'UiArt.cs')
[UiArt]::RenderAll([System.IO.Path]::GetFullPath($OutDir))
