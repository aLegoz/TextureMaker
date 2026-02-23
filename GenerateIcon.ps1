Add-Type -AssemblyName System.Drawing

function New-IconImage([int]$Size) {
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad    = [Math]::Max(1, [int]($Size * 0.04))
    $radius = [int]($Size * 0.20)
    $rx = $pad; $ry = $pad
    $rw = $Size - $pad * 2
    $rh = $Size - $pad * 2

    # Rounded rect path
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rx,                 $ry,                 $radius*2, $radius*2, 180, 90)
    $path.AddArc($rx+$rw-$radius*2,  $ry,                 $radius*2, $radius*2, 270, 90)
    $path.AddArc($rx+$rw-$radius*2,  $ry+$rh-$radius*2,  $radius*2, $radius*2,   0, 90)
    $path.AddArc($rx,                 $ry+$rh-$radius*2,  $radius*2, $radius*2,  90, 90)
    $path.CloseFigure()

    # Gradient background
    $pt1 = New-Object System.Drawing.PointF; $pt1.X = $rx; $pt1.Y = $ry
    $pt2 = New-Object System.Drawing.PointF; $pt2.X = $rx; $pt2.Y = ($ry + $rh)
    $gbr = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $pt1, $pt2,
        [System.Drawing.Color]::FromArgb(255, 50, 50, 50),
        [System.Drawing.Color]::FromArgb(255, 28, 28, 28))
    $g.FillPath($gbr, $path)
    $gbr.Dispose()

    # Border
    if ($Size -ge 32) {
        $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(100, 140, 140, 140), 1)
        $g.DrawPath($pen, $path)
        $pen.Dispose()
    }

    # Texture grid lines (48+)
    if ($Size -ge 48) {
        $step = [int]($Size / 8)
        $lp   = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(20, 255, 255, 255), 1)
        $g.SetClip($path)
        for ($i = $step; $i -lt $Size; $i += $step) {
            $g.DrawLine($lp, $i, $ry, $i, $ry+$rh)
            $g.DrawLine($lp, $rx, $i, $rx+$rw, $i)
        }
        $g.ResetClip()
        $lp.Dispose()
    }

    # "T" letter
    $fs   = [float]($Size * 0.58)
    $font = New-Object System.Drawing.Font('Segoe UI', $fs, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $sf   = New-Object System.Drawing.StringFormat
    $sf.Alignment     = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $dr   = New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)

    if ($Size -ge 32) {
        $sx = [float]($Size * 0.025); $sy = [float]($Size * 0.030)
        $sr = New-Object System.Drawing.RectangleF($sx, $sy, $Size, $Size)
        $sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
        $g.DrawString('T', $font, $sb, $sr, $sf)
        $sb.Dispose()
    }

    $tb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 240, 168, 32))
    $g.DrawString('T', $font, $tb, $dr, $sf)
    $tb.Dispose()
    $font.Dispose()
    $path.Dispose()
    $g.Dispose()
    return $bmp
}

# Collect PNG bytes in a .NET List to avoid PowerShell array-unrolling
$pngList = New-Object System.Collections.Generic.List[byte[]]
$sizes   = @(16, 32, 48, 256)

foreach ($s in $sizes) {
    $bmp = New-IconImage -Size $s
    $ms  = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngList.Add($ms.ToArray())
    $ms.Dispose()
    $bmp.Dispose()
    Write-Host "  Generated ${s}x${s}  ($($pngList[$pngList.Count-1].Length) bytes)"
}

# Build .ico
$count      = $sizes.Count
$dataOffset = 6 + 16 * $count

$out = New-Object System.IO.MemoryStream
$bw  = New-Object System.IO.BinaryWriter($out)

# ICONDIR
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$count)

# ICONDIRENTRYs
$offset = $dataOffset
for ($i = 0; $i -lt $count; $i++) {
    $wh = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
    $bw.Write([byte]$wh)
    $bw.Write([byte]$wh)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$pngList[$i].Length)
    $bw.Write([uint32]$offset)
    $offset += $pngList[$i].Length
}

# PNG blobs
for ($i = 0; $i -lt $count; $i++) {
    $bw.Write($pngList[$i])
}

$bw.Flush()
$bytes = $out.ToArray()
$bw.Dispose(); $out.Dispose()

$dest = 'D:\Projects\TextureMaker\TextureMaker\Resources\TextureMaker.ico'
[System.IO.File]::WriteAllBytes($dest, $bytes)
Write-Host "Saved: $dest  ($([Math]::Round($bytes.Length/1024,1)) KB)"
