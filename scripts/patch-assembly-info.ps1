# patch-assembly-info.ps1
# Replicates Build.fsx targets: patch_commit, patch_ver, patch_date
# and AssemblyPatcher.fs logic for SharedAssemblyInfo.cs + FastScaling AssemblyInfo.cpp

param(
    [string]$BuildNumber = ""
)

$ErrorActionPreference = "Stop"

$rootDir = (Resolve-Path "$PSScriptRoot\..").Path
$assemblyInfoFile = Join-Path $rootDir "Core\SharedAssemblyInfo.cs"
$assemblyInfoCppFile = Join-Path $rootDir "Plugins\FastScaling\AssemblyInfo.cpp"

# --- Helper functions (mirrors AssemblyPatcher.fs) ---

function Get-AssemblyInfo {
    param([string]$File, [string]$Key)
    $content = Get-Content $File -Raw -Encoding UTF8
    $pattern = "\[assembly:\s*${Key}\s*\(`"([^`"]*)"
    if ($content -match $pattern) {
        return $Matches[1]
    }
    return ""
}

function Set-AssemblyInfo {
    param([string]$File, [hashtable]$KeyValues)
    $content = Get-Content $File -Raw -Encoding UTF8
    foreach ($kv in $KeyValues.GetEnumerator()) {
        $key = $kv.Key
        $value = $kv.Value
        $pattern = "(\[assembly:\s*${key}[^\]]*\])"
        $replacement = "[assembly: ${key}(`"${value}`")]"
        $newContent = [regex]::Replace($content, $pattern, $replacement)
        if ($newContent.Contains($replacement)) {
            $content = $newContent
        } else {
            # Attribute not found - append it (mirrors AssemblyPatcher.fs line 19)
            $content = $content + $replacement + "`n"
        }
    }
    [System.IO.File]::WriteAllText($File, $content, [System.Text.UTF8Encoding]::new($false))
}

function Set-CppAssemblyInfo {
    param([string]$File, [hashtable]$KeyValues)
    if (-not (Test-Path $File)) {
        Write-Host "  Skipping $File (not found)"
        return
    }
    $content = Get-Content $File -Raw -Encoding UTF8
    foreach ($kv in $KeyValues.GetEnumerator()) {
        $key = $kv.Key
        $value = $kv.Value
        # C++ uses [assembly:KeyAttribute(L"value")] or [assembly:KeyAttribute("value")]
        $pattern = "(\[assembly:\s*${key}[^\]]*\])"
        $replacement = "[assembly: ${key}(`"${value}`")]"
        $newContent = [regex]::Replace($content, $pattern, $replacement)
        if ($newContent.Contains($replacement)) {
            $content = $newContent
        } else {
            $content = $content + $replacement + "`n"
        }
    }
    [System.IO.File]::WriteAllText($File, $content, [System.Text.UTF8Encoding]::new($false))
}

# --- Parse current version from SharedAssemblyInfo.cs ---

$infoVer = Get-AssemblyInfo $assemblyInfoFile "AssemblyInformationalVersion"
Write-Host "Current AssemblyInformationalVersion: $infoVer"

# Parse semver: Major.Minor.Patch[-prerelease]
if ($infoVer -match "^(\d+)\.(\d+)\.(\d+)(-(.+))?$") {
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $patch = [int]$Matches[3]
    $prerelease = if ($Matches[5]) { $Matches[5] } else { "prerelease" }
} else {
    Write-Error "Cannot parse version from SharedAssemblyInfo.cs: '$infoVer'"
    exit 1
}

Write-Host "Parsed version: $major.$minor.$patch-$prerelease"

# --- Determine build number ---

if (-not $BuildNumber) {
    if ($env:GITHUB_RUN_NUMBER) {
        $BuildNumber = $env:GITHUB_RUN_NUMBER
    } else {
        # Fallback like Build.fsx: use time-based number
        $BuildNumber = ([DateTimeOffset]::UtcNow.TimeOfDay.Milliseconds % [int16]::MaxValue).ToString()
    }
}
Write-Host "Build number: $BuildNumber"

# --- Detect git tag for release builds (mirrors Build.fsx lines 97-114) ---

$isRelease = $false
$gitTag = $null

# Check GITHUB_REF for tag pushes
if ($env:GITHUB_REF -and $env:GITHUB_REF.StartsWith("refs/tags/")) {
    $gitTag = $env:GITHUB_REF -replace "^refs/tags/", ""
}

# Fallback: check if current commit has an exact tag
if (-not $gitTag) {
    try {
        $tagResult = & git describe --tags --exact-match --abbrev=0 2>$null
        if ($LASTEXITCODE -eq 0 -and $tagResult) {
            $gitTag = $tagResult.Trim()
        }
    } catch { }
}

# Trim leading 'v' from tag (mirrors Build.fsx trimV)
function TrimV([string]$s) { return $s -replace "^v", "" }

if ($gitTag) {
    $trimmed = TrimV $gitTag
    if ($trimmed -match "^\d+\.\d+\.\d+") {
        $isRelease = $true
        Write-Host "Release build from git tag: $gitTag -> $trimmed"
        if ($trimmed -match "^(\d+)\.(\d+)\.(\d+)(-(.+))?$") {
            $major = [int]$Matches[1]
            $minor = [int]$Matches[2]
            $patch = [int]$Matches[3]
            $prerelease = if ($Matches[5]) { $Matches[5] } else { "" }
        }
    } else {
        Write-Host "Warning: git tag '$gitTag' is not valid semver; not processing as release"
    }
} else {
    Write-Host "No git tag found on this commit."
}

# --- Compute version strings (mirrors Build.fsx patch_ver) ---

# AssemblyVersion: Major.0.0.0 (only major changes break binding)
$asmVer = "$major.0.0.0"

# AssemblyFileVersion: Major.Minor.Patch.BuildNo
$fileVer = "$major.$minor.$patch.$BuildNumber"

# AssemblyInformationalVersion: full semver with prerelease + build
if ($isRelease) {
    if ($prerelease) {
        $infoVersion = "$major.$minor.$patch-$prerelease"
    } else {
        $infoVersion = "$major.$minor.$patch"
    }
} else {
    $infoVersion = "$major.$minor.$patch-${prerelease}.$BuildNumber"
}

# NuGet version (no build metadata for nuget)
if ($isRelease) {
    if ($prerelease) {
        $nugetVersion = "$major.$minor.$patch-$prerelease"
    } else {
        $nugetVersion = "$major.$minor.$patch"
    }
} else {
    $paddedBuild = $BuildNumber.PadLeft(4, '0')
    $nugetVersion = "$major.$minor.$patch-${prerelease}${paddedBuild}"
}

Write-Host ""
Write-Host "Version computations:"
Write-Host "  AssemblyVersion:              $asmVer"
Write-Host "  AssemblyFileVersion:          $fileVer"
Write-Host "  AssemblyInformationalVersion: $infoVersion"
Write-Host "  NuGet version:                $nugetVersion"
Write-Host "  Is release:                   $isRelease"

# --- patch_commit: write git SHA ---

$commit = ""
try {
    $commit = (& git rev-parse HEAD 2>$null).Trim()
} catch { }

if ($commit) {
    Write-Host ""
    Write-Host "Patching commit SHA: $commit"
    Set-AssemblyInfo $assemblyInfoFile @{ "Commit" = $commit }
    Set-CppAssemblyInfo $assemblyInfoCppFile @{ "CommitAttribute" = $commit }
} else {
    Write-Host "Warning: Could not determine git commit SHA"
}

# --- patch_ver: write version attributes ---

Write-Host "Patching version attributes in SharedAssemblyInfo.cs"
Set-AssemblyInfo $assemblyInfoFile @{
    "AssemblyVersion"              = $asmVer
    "AssemblyFileVersion"          = $fileVer
    "AssemblyInformationalVersion" = $infoVersion
}

Write-Host "Patching version attributes in AssemblyInfo.cpp"
Set-CppAssemblyInfo $assemblyInfoCppFile @{
    "AssemblyVersionAttribute"              = $asmVer
    "AssemblyFileVersionAttribute"          = $fileVer
    "AssemblyInformationalVersionAttribute" = $infoVersion
}

# --- patch_date: write build date ---

$buildDate = [DateTimeOffset]::UtcNow.ToString("o")
Write-Host "Patching build date: $buildDate"
Set-AssemblyInfo $assemblyInfoFile @{ "BuildDate" = $buildDate }
Set-CppAssemblyInfo $assemblyInfoCppFile @{ "BuildDateAttribute" = $buildDate }

# --- Export outputs for GitHub Actions ---

if ($env:GITHUB_OUTPUT) {
    "nuget_version=$nugetVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "informational_version=$infoVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "is_release=$($isRelease.ToString().ToLower())" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    Write-Host ""
    Write-Host "Exported to GITHUB_OUTPUT: nuget_version=$nugetVersion, is_release=$isRelease"
}

Write-Host ""
Write-Host "Assembly info patching complete."
