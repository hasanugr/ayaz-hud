# Uygulama simgesini (src/AyazHUD/Varliklar/simge.ico) uretir.
# Koyu yuvarlak kare uzerinde 270 derecelik halka olcer; birden cok boyut tek .ico icinde PNG olarak saklanir.
# Kullanim: powershell -ExecutionPolicy Bypass -File araclar\simge-uret.ps1
param(
  [string]$Cikti = (Join-Path $PSScriptRoot '..\src\AyazHUD\Varliklar\simge.ico')
)

Add-Type -AssemblyName System.Drawing

function Kare([int]$S) {
  $bmp = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.Clear([System.Drawing.Color]::Transparent)

  # Zemin: koyu, hafif dikey gecisli yuvarlak kare
  $r = [Math]::Max(2, [int]($S * 0.22))
  $yol = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = 2 * $r
  $w = $S - 1
  $yol.AddArc(0, 0, $d, $d, 180, 90)
  $yol.AddArc($w - $d, 0, $d, $d, 270, 90)
  $yol.AddArc($w - $d, $w - $d, $d, $d, 0, 90)
  $yol.AddArc(0, $w - $d, $d, $d, 90, 90)
  $yol.CloseFigure()
  $zemin = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 0, $S), ([System.Drawing.Color]::FromArgb(255, 24, 30, 46)), ([System.Drawing.Color]::FromArgb(255, 10, 12, 19))
  $g.FillPath($zemin, $yol)

  # Halka: iz + dolu kisim (vurgu rengi)
  $kalinlik = if ($S -le 16) { 2.6 } elseif ($S -le 24) { 3.2 } else { $S * 0.105 }
  $yaricap = $S * 0.31
  $mx = $S / 2.0
  $my = $S / 2.0 + $S * 0.035
  $kutu = New-Object System.Drawing.RectangleF ($mx - $yaricap), ($my - $yaricap), (2 * $yaricap), (2 * $yaricap)
  $iz = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 48, 56, 74)), $kalinlik
  $iz.StartCap = 'Round'; $iz.EndCap = 'Round'
  $g.DrawArc($iz, $kutu, 135, 270)
  $dolu = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 125, 211, 252)), $kalinlik
  $dolu.StartCap = 'Round'; $dolu.EndCap = 'Round'
  $g.DrawArc($dolu, $kutu, 135, 190)

  # Ortada kucuk nokta (buyuk boyutlarda)
  if ($S -ge 32) {
    $n = $S * 0.075
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))), ($mx - $n), ($my - $n), (2 * $n), (2 * $n))
  }

  $g.Dispose()
  $ms = New-Object System.IO.MemoryStream
  if ($S -ge 128) {
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  } else {
    # Kucuk boyutlar klasik DIB olarak: .NET Framework'un Icon sinifi PNG karelerini kucuk boyutta okuyamiyor
    $veri = $bmp.LockBits((New-Object System.Drawing.Rectangle 0, 0, $S, $S), 'ReadOnly', ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb))
    $piksel = New-Object byte[] ($S * $S * 4)
    [System.Runtime.InteropServices.Marshal]::Copy($veri.Scan0, $piksel, 0, $piksel.Length)
    $bmp.UnlockBits($veri)
    $y = New-Object System.IO.BinaryWriter $ms
    $y.Write([UInt32]40); $y.Write([Int32]$S); $y.Write([Int32]($S * 2))
    $y.Write([UInt16]1); $y.Write([UInt16]32); $y.Write([UInt32]0)
    $y.Write([UInt32]($S * $S * 4)); $y.Write([Int32]0); $y.Write([Int32]0); $y.Write([UInt32]0); $y.Write([UInt32]0)
    for ($satir = $S - 1; $satir -ge 0; $satir--) { $y.Write($piksel, $satir * $S * 4, $S * 4) }
    $maske = [int]([Math]::Ceiling($S / 32.0) * 4)
    $y.Write((New-Object byte[] ($maske * $S)))
    $y.Flush()
  }
  $bmp.Dispose()
  return ,$ms.ToArray()
}

$boyutlar = 16, 20, 24, 32, 40, 48, 64, 128, 256
$resimler = @($boyutlar | ForEach-Object { ,(Kare $_) })

$akis = New-Object System.IO.MemoryStream
$yazici = New-Object System.IO.BinaryWriter $akis
$yazici.Write([UInt16]0); $yazici.Write([UInt16]1); $yazici.Write([UInt16]$boyutlar.Count)
$konum = 6 + 16 * $boyutlar.Count
for ($i = 0; $i -lt $boyutlar.Count; $i++) {
  $s = $boyutlar[$i]
  $b = if ($s -ge 256) { 0 } else { $s }
  $yazici.Write([byte]$b); $yazici.Write([byte]$b); $yazici.Write([byte]0); $yazici.Write([byte]0)
  $yazici.Write([UInt16]1); $yazici.Write([UInt16]32)
  $yazici.Write([UInt32]$resimler[$i].Length); $yazici.Write([UInt32]$konum)
  $konum += $resimler[$i].Length
}
foreach ($r in $resimler) { $yazici.Write($r) }
$yazici.Flush()

$tam = [System.IO.Path]::GetFullPath($Cikti)
New-Item -ItemType Directory -Force (Split-Path $tam) | Out-Null
[System.IO.File]::WriteAllBytes($tam, $akis.ToArray())
"Yazildi: $tam ($($akis.Length) bayt)"
