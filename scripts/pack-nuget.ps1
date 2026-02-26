# pack-nuget.ps1
# Replicates Build.fsx pack_nuget target and Nuget.fs fillVariables/pack
# Performs $token$ substitution on .nuspec files, then calls nuget pack

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [switch]$IncludeFastScaling
)

$ErrorActionPreference = "Continue"

$rootDir = (Resolve-Path "$PSScriptRoot\..").Path
$nugetDir = Join-Path $rootDir "nuget"
$tmpDir = Join-Path $rootDir "tmp"
$outDir = Join-Path $rootDir "Releases\nuget-packages"
$dllDir = Join-Path $rootDir "dlls\release"

Write-Host "NuGet packaging"
Write-Host "  Version:  $Version"
Write-Host "  Root:     $rootDir"
Write-Host "  DLL dir:  $dllDir"
Write-Host "  Output:   $outDir"
Write-Host ""

# --- Clean and create directories ---

if (Test-Path $tmpDir) { Remove-Item $tmpDir -Recurse -Force }
New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# --- Token values (mirrors Build.fsx nvc list, lines 234-245) ---

$tokens = @{
    "version"       = $Version
    "author"        = "Nathanael Jones, Imazen"
    "owners"        = "nathanaeljones, imazen"
    "pluginsdlldir" = $dllDir
    "coredlldir"    = $dllDir
    "iconurl"       = "http://imageresizing.net/images/logos/ImageIconPSD100.png"
    "plugins"       = @"
## 30+ plugins available

Search 'ImageResizer' on nuget.org, or visit imageresizing.net to see 40+ plugins. Some offer 4-30x performance improvements; some render PDFs and PSDs; others detect faces and trim whitespace.
You'll find  plugins for disk caching, memory caching, Microsoft SQL blob support, Amazon CloudFront, S3, Azure Blob Storage, MongoDB GridFS, automatic whitespace trimming, automatic white balance, octree 8-bit gif/png quantization and transparency dithering, animated gif resizing, watermark &amp; text overlay support, content aware image resizing / seam carving (based on CAIR), grayscale, sepia, histogram, alpha, contrast, saturation, brightness, hue, Guassian blur, noise removal, and smart sharpen filters, psd editing &amp; rendering, raw (CR2, NEF, DNG, etc.) file exposure, .webp (weppy) support, image batch processing &amp; compression into .zip archives, red eye auto-correction,  face detection, and secure (signed!) remote HTTP image processing. Most datastore plugins support the Virtual Path Provider system, and can be used for non-image files as well.

"@
}

# --- fillVariables: regex substitution on .nuspec files (mirrors Nuget.fs line 16) ---

$nuspecFiles = Get-ChildItem -Path $nugetDir -Filter "*.nuspec"
Write-Host "Found $($nuspecFiles.Count) .nuspec files in $nugetDir"

$skippedFastScaling = @()

foreach ($nuspec in $nuspecFiles) {
    # Skip FastScaling nuspecs unless explicitly included
    if (-not $IncludeFastScaling -and $nuspec.Name -match "FastScaling") {
        $skippedFastScaling += $nuspec.Name
        continue
    }

    $content = [System.IO.File]::ReadAllText($nuspec.FullName, [System.Text.UTF8Encoding]::new($false))

    foreach ($kv in $tokens.GetEnumerator()) {
        # Mirrors Nuget.fs: Regex.Replace(fileContents, "\\$"+key+"\\$", value)
        $pattern = [regex]::Escape("`$" + $kv.Key + "`$")
        $content = [regex]::Replace($content, $pattern, $kv.Value)
    }

    $outPath = Join-Path $tmpDir $nuspec.Name
    [System.IO.File]::WriteAllText($outPath, $content, [System.Text.UTF8Encoding]::new($false))
}

if ($skippedFastScaling.Count -gt 0) {
    Write-Host "Skipped FastScaling nuspecs: $($skippedFastScaling -join ', ')"
}

# --- nuget pack each processed .nuspec ---

$processedSpecs = Get-ChildItem -Path $tmpDir -Filter "*.nuspec"
$succeeded = 0
$failed = 0
$failedNames = @()

Write-Host ""
Write-Host "Packing $($processedSpecs.Count) .nuspec files..."
Write-Host ""

foreach ($nuspec in $processedSpecs) {
    Write-Host "  Packing $($nuspec.Name)..."
    $args = @("pack", "-Version", $Version, "-OutputDirectory", $outDir, $nuspec.FullName)

    $output = & nuget @args 2>&1
    if ($LASTEXITCODE -eq 0) {
        $succeeded++
    } else {
        $failed++
        $failedNames += $nuspec.Name
        Write-Host "    WARNING: nuget pack failed for $($nuspec.Name)" -ForegroundColor Yellow
        Write-Host "    $output" -ForegroundColor Yellow
    }
}

# --- Clean up temp directory ---

Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue

# --- Summary ---

Write-Host ""
Write-Host "========================================="
Write-Host "NuGet Packaging Summary"
Write-Host "========================================="
Write-Host "  Succeeded: $succeeded"
Write-Host "  Failed:    $failed"

if ($failedNames.Count -gt 0) {
    Write-Host "  Failed packages:" -ForegroundColor Yellow
    foreach ($name in $failedNames) {
        Write-Host "    - $name" -ForegroundColor Yellow
    }
}

$packages = Get-ChildItem -Path $outDir -Filter "*.nupkg" -ErrorAction SilentlyContinue
if ($packages) {
    Write-Host ""
    Write-Host "Created packages ($($packages.Count)):"
    foreach ($pkg in $packages | Sort-Object Name) {
        $sizeMB = [math]::Round($pkg.Length / 1KB, 1)
        Write-Host "  ${sizeMB}KB  $($pkg.Name)"
    }
} else {
    Write-Host ""
    Write-Host "WARNING: No .nupkg files were created!" -ForegroundColor Yellow
}

Write-Host ""

# Don't fail the build if some packages failed - some plugins may reference optional DLLs
if ($succeeded -eq 0) {
    Write-Error "All NuGet packages failed to build!"
    exit 1
}
