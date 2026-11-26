# Raises the minor version in the <Version> element of an MSBuild props or project file.
# Major.Minor.Patch becomes Major.(Minor+1).0, encoding (BOM) and line endings of the file stay untouched.
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Warning "File not found: $Path"
    exit 1
}

$bytes = [System.IO.File]::ReadAllBytes($Path)
$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
$text = [System.Text.Encoding]::UTF8.GetString($bytes)

$regex = [regex]'<Version>(\d+)\.(\d+)(?:\.\d+)*</Version>'
$match = $regex.Match($text)

if (-not $match.Success) {
    Write-Warning "No <Version>major.minor[.patch]</Version> element found in $Path"
    exit 1
}

$oldVersion = $match.Value -replace '</?Version>', ''
$newVersion = '{0}.{1}.0' -f $match.Groups[1].Value, ([int]$match.Groups[2].Value + 1)

$text = $regex.Replace($text, "<Version>$newVersion</Version>", 1)

$encoding = New-Object System.Text.UTF8Encoding($hasBom)
[System.IO.File]::WriteAllText($Path, $text, $encoding)

Write-Host "Version bumped: $oldVersion -> $newVersion"
