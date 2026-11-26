# Raises the minor version in an MSBuild project file that keeps its version in
# <VersionMajor>, <VersionMinor> and <VersionBuild>. The build number is reset to 0.
# Encoding (BOM) and line endings of the file stay untouched.
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

$majorRegex = [regex]'<VersionMajor>(\d+)</VersionMajor>'
$minorRegex = [regex]'<VersionMinor>(\d+)</VersionMinor>'
$buildRegex = [regex]'<VersionBuild>(\d+)</VersionBuild>'

$majorMatch = $majorRegex.Match($text)
$minorMatch = $minorRegex.Match($text)
$buildMatch = $buildRegex.Match($text)

if (-not $majorMatch.Success -or -not $minorMatch.Success -or -not $buildMatch.Success) {
    Write-Warning "<VersionMajor>, <VersionMinor> and <VersionBuild> must all be present in $Path"
    exit 1
}

$major = $majorMatch.Groups[1].Value
$oldVersion = '{0}.{1}.{2}' -f $major, $minorMatch.Groups[1].Value, $buildMatch.Groups[1].Value
$newMinor = [int]$minorMatch.Groups[1].Value + 1
$newVersion = '{0}.{1}.0' -f $major, $newMinor

$text = $minorRegex.Replace($text, "<VersionMinor>$newMinor</VersionMinor>", 1)
$text = $buildRegex.Replace($text, '<VersionBuild>0</VersionBuild>', 1)

$encoding = New-Object System.Text.UTF8Encoding($hasBom)
[System.IO.File]::WriteAllText($Path, $text, $encoding)

Write-Host "Version bumped: $oldVersion -> $newVersion"
