Add-Type -AssemblyName System.Drawing

$assets = "C:\Users\M.D.S\Desktop\DIHub\DIHub.APP\Assets"
$src = Join-Path $assets "Square310x310Logo.png"

if (-not (Test-Path $src)) {
    Write-Host "❌ Square310x310Logo.png not found in $assets" -ForegroundColor Red
    exit 1
}

$img = [System.Drawing.Image]::FromFile($src)

function Gen($size, $name) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.SmoothingMode = 'HighQuality'
    $g.DrawImage($img, 0, 0, $size, $size)
    $g.Dispose()
    $bmp.Save((Join-Path $assets $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  ✅ $name" -ForegroundColor Green
}

Write-Host "Generating icons..." -ForegroundColor Cyan

# Targetsize بدون suffix (برای Start Menu بزرگ)
Gen 16 "Square44x44Logo.targetsize-16.png"
Gen 24 "Square44x44Logo.targetsize-24.png"
Gen 32 "Square44x44Logo.targetsize-32.png"
Gen 48 "Square44x44Logo.targetsize-48.png"
Gen 256 "Square44x44Logo.targetsize-256.png"

# Targetsize با suffix altform-unplated (برای Desktop و Taskbar)
Gen 16 "Square44x44Logo.targetsize-16_altform-unplated.png"
Gen 24 "Square44x44Logo.targetsize-24_altform-unplated.png"
Gen 32 "Square44x44Logo.targetsize-32_altform-unplated.png"
Gen 48 "Square44x44Logo.targetsize-48_altform-unplated.png"
Gen 256 "Square44x44Logo.targetsize-256_altform-unplated.png"

# Scale variants
Gen 44 "Square44x44Logo.scale-100.png"
Gen 88 "Square44x44Logo.scale-200.png"
Gen 150 "Square150x150Logo.scale-100.png"
Gen 300 "Square150x150Logo.scale-200.png"
Gen 50 "StoreLogo.scale-100.png"
Gen 100 "StoreLogo.scale-200.png"

$img.Dispose()
Write-Host "`n✅ Done!" -ForegroundColor Green

Get-ChildItem $assets -Filter "Square44x44Logo*" | Sort-Object Name | Select-Object Name