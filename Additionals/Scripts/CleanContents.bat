@echo off
:: Removes build and test artifacts (bin, obj, TestResults, .vs, packages, *.tmp, *.log, *.cache).
:: Place this file two levels below the solution root. Use "Clean-Project.bat -WhatIf" for a dry run.
:: The PowerShell part below the marker line is executed by the launcher line; cmd never reads it.
set "CLEAN_DIR=%~dp0"
set "CLEAN_ARGS=%*"
set "PSEXE=powershell"

where pwsh >nul 2>&1 && set "PSEXE=pwsh"
%PSEXE% -NoProfile -ExecutionPolicy Bypass -Command "$s = Get-Content -LiteralPath '%~f0' -Raw; $i = $s.IndexOf('#PS' + 'START'); & ([scriptblock]::Create($s.Substring($i)))"
echo.
pause
exit /b
#PSSTART
$ErrorActionPreference = 'Stop'

$whatIf = $env:CLEAN_ARGS -match '-WhatIf'
$root = (Resolve-Path (Join-Path $env:CLEAN_DIR '..\..')).Path

# Folder names removed everywhere below the root
$folderNames = @('bin', 'obj', 'TestResults')

# File patterns removed everywhere below the root
$filePatterns = @('*.tmp', '*.log', '*.cache')

# Path segments that are never touched
$excludedSegments = @('\.git\', '\node_modules\')

$deletedFolders = 0
$deletedFiles = 0
$errors = 0

function Test-Excluded([string]$path) {
    $probe = $path + '\'
    foreach ($segment in $excludedSegments) {
        if ($probe.Contains($segment)) { return $true }
    }
    return $false
}

# Extended-length prefix so paths over 260 characters work in Windows PowerShell 5.1 too
function Get-LongPath([string]$path) {
    if ($path.StartsWith('\\?\')) { return $path }
    if ($path.StartsWith('\\')) { return '\\?\UNC\' + $path.Substring(2) }
    return '\\?\' + $path
}

function Remove-Artifact([string]$path, [bool]$isFolder) {
    $long = Get-LongPath $path

    # Parent may already be gone when nested matches (bin\...\obj) were collected
    if (-not (Test-Path -LiteralPath $long)) { return }

    if ($whatIf) {
        Write-Host "[WhatIf] $path"
        return
    }

    try {
        if ($isFolder) {
            Remove-Item -LiteralPath $long -Recurse -Force -ErrorAction Stop
            Write-Host "Deleted: $path"
            $script:deletedFolders++
        }
        else {
            Remove-Item -LiteralPath $long -Force -ErrorAction Stop
            $script:deletedFiles++
        }
    }
    catch {
        Write-Host "[ERROR] $path : $($_.Exception.Message)" -ForegroundColor Red
        $script:errors++
    }
}

Write-Host '================================================'
Write-Host 'C# Project Cleanup Script'
Write-Host '================================================'
Write-Host "Root: $root"

# Warn if no project files are present in the root
$hasProjectFiles = Get-ChildItem -LiteralPath $root -File -Force |
    Where-Object { $_.Extension -in '.sln', '.csproj', '.vbproj', '.fsproj' } |
    Select-Object -First 1

if (-not $hasProjectFiles) {
    Write-Warning 'No Visual Studio project files found in root.'
    $answer = Read-Host 'Continue anyway? [y/n]'
    if ($answer -notin 'y', 'yes') {
        Write-Host 'Cancelled.'
        return
    }
}

Write-Host ''
Write-Host '[INFO] Searching for build and test artifacts...'

# Shortest paths first, so parents are deleted before their nested matches
$folders = Get-ChildItem -LiteralPath $root -Recurse -Directory -Force -ErrorAction SilentlyContinue |
    Where-Object { $folderNames -contains $_.Name -and -not (Test-Excluded $_.FullName) } |
    Sort-Object { $_.FullName.Length }

foreach ($folder in $folders) {
    Remove-Artifact $folder.FullName $true
}

# Old NuGet structure (root level only)
$packages = Join-Path $root 'packages'
if (Test-Path -LiteralPath $packages -PathType Container) {
    Remove-Artifact $packages $true
}

Write-Host ''
Write-Host '[INFO] Deleting temporary files...'

$files = Get-ChildItem -LiteralPath $root -Recurse -File -Force -Include $filePatterns -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.DirectoryName) }

foreach ($file in $files) {
    Remove-Artifact $file.FullName $false
}

Write-Host ''
Write-Host '================================================'
Write-Host 'Cleanup completed!'
Write-Host '================================================'
Write-Host "Deleted folders: $deletedFolders"
Write-Host "Deleted files:   $deletedFiles"
if ($errors -gt 0) {
    Write-Host "Errors:          $errors" -ForegroundColor Red
}