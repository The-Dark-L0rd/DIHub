# ═══════════════════════════════════════════════════════════
#  DI Hub — Generate all required PNG variants for MSIX
# ═══════════════════════════════════════════════════════════

Add-Type -AssemblyName System.Drawing

$assetsPath = "C:\Users\M.D.S\Desktop\DIHub\DIHub.APP\Assets"
$sourcePng = Join-Path $assetsPath "Square310x310Logo.png"

if (-not (Test-Path $sourcePng)) {
    Write-Error "Source PNG not found: $sourcePng"
    exit 1
}

$source = [System.Drawing.Image]::FromFile($sourcePng)

function Save-Resized {
    param([int]$Size, [string]$Filename)
    
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.DrawImage($source, 0, 0, $Size, $Size)
    $g.Dispose()
    
    $path = Join-Path $assetsPath $Filename
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    
    Write-Host "  ✅ $Filename ($Size x $Size)" -ForegroundColor Green
}

Write-Host "`n🎨 Generating Square44x44Logo targetsize variants..." -ForegroundColor Cyan
Save-Resized -Size 16  -Filename "Square44x44Logo.targetsize-16.png"
Save-Resized -Size 20  -Filename "Square44x44Logo.targetsize-20.png"
Save-Resized -Size 24  -Filename "Square44x44Logo.targetsize-24.png"
Save-Resized -Size 30  -Filename "Square44x44Logo.targetsize-30.png"
Save-Resized -Size 32  -Filename "Square44x44Logo.targetsize-32.png"
Save-Resized -Size 36  -Filename "Square44x44Logo.targetsize-36.png"
Save-Resized -Size 40  -Filename "Square44x44Logo.targetsize-40.png"
Save-Resized -Size 44  -Filename "Square44x44Logo.targetsize-44.png"
Save-Resized -Size 48  -Filename "Square44x44Logo.targetsize-48.png"
Save-Resized -Size 60  -Filename "Square44x44Logo.targetsize-60.png"
Save-Resized -Size 64  -Filename "Square44x44Logo.targetsize-64.png"
Save-Resized -Size 72  -Filename "Square44x44Logo.targetsize-72.png"
Save-Resized -Size 80  -Filename "Square44x44Logo.targetsize-80.png"
Save-Resized -Size 96  -Filename "Square44x44Logo.targetsize-96.png"
Save-Resized -Size 256 -Filename "Square44x44Logo.targetsize-256.png"

Write-Host "`n🎨 Generating Square44x44Logo altform variants..." -ForegroundColor Cyan
Save-Resized -Size 16  -Filename "Square44x44Logo.targetsize-16_altform-unplated.png"
Save-Resized -Size 24  -Filename "Square44x44Logo.targetsize-24_altform-unplated.png"
Save-Resized -Size 32  -Filename "Square44x44Logo.targetsize-32_altform-unplated.png"
Save-Resized -Size 48  -Filename "Square44x44Logo.targetsize-48_altform-unplated.png"
Save-Resized -Size 256 -Filename "Square44x44Logo.targetsize-256_altform-unplated.png"

Write-Host "`n🎨 Generating Square44x44Logo scale variants..." -ForegroundColor Cyan
Save-Resized -Size 44  -Filename "Square44x44Logo.scale-100.png"
Save-Resized -Size 88  -Filename "Square44x44Logo.scale-200.png"
Save-Resized -Size 176 -Filename "Square44x44Logo.scale-400.png"

Write-Host "`n🎨 Generating Square150x150Logo scale variants..." -ForegroundColor Cyan
Save-Resized -Size 150 -Filename "Square150x150Logo.scale-100.png"
Save-Resized -Size 300 -Filename "Square150x150Logo.scale-200.png"

Write-Host "`n🎨 Generating SplashScreen scale variants..." -ForegroundColor Cyan
Save-Resized -Size 620 -Filename "SplashScreen.scale-100.png"
Save-Resized -Size 1240 -Filename "SplashScreen.scale-200.png"

Write-Host "`n🎨 Generating StoreLogo scale variants..." -ForegroundColor Cyan
Save-Resized -Size 50  -Filename "StoreLogo.scale-100.png"
Save-Resized -Size 100 -Filename "StoreLogo.scale-200.png"

$source.Dispose()

Write-Host "`n🎉 All icon variants generated successfully!" -ForegroundColor Green
Write-Host "Location: $assetsPath`n"

# نمایش لیست نهایی
Get-ChildItem $assetsPath -Filter "*.png" | Sort-Object Name | Select-Object Name, Length