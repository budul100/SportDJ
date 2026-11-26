# Creates a Windows icon with the sizes 16 to 256 px from a square png.
# Usage: .\Additionals\Icons\New-Icon.ps1
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\..\App\Assets\SportDJ.png'),
    [string]$Target = (Join-Path $PSScriptRoot '..\..\App\Assets\SportDJ.ico')
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$targetPath = [System.IO.Path]::GetFullPath($Target)

$image = [System.Drawing.Image]::FromFile($sourcePath)

try {
    if ($image.Width -ne $image.Height) {
        throw "The source image must be square, but it is $($image.Width)x$($image.Height)."
    }

    $entries = foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $attributes = New-Object System.Drawing.Imaging.ImageAttributes

        try {
            $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)

            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

            $destination = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
            $graphics.DrawImage($image, $destination, 0, 0, $image.Width, $image.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)

            $stream = New-Object System.IO.MemoryStream
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)

            [pscustomobject]@{ Size = $size; Data = $stream.ToArray() }

            $stream.Dispose()
        }
        finally {
            $attributes.Dispose()
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
}
finally {
    $image.Dispose()
}

$file = [System.IO.File]::Create($targetPath)
$writer = New-Object System.IO.BinaryWriter($file)

try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$entries.Count)

    $offset = 6 + 16 * $entries.Count

    foreach ($entry in $entries) {
        $dimension = if ($entry.Size -ge 256) { 0 } else { $entry.Size }

        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$entry.Data.Length)
        $writer.Write([uint32]$offset)

        $offset += $entry.Data.Length
    }

    foreach ($entry in $entries) {
        $writer.Write([byte[]]$entry.Data)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Write-Host "Created $targetPath with the sizes $($sizes -join ', ')"
