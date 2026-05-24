#!/bin/pwsh

<#
.SYNOPSIS
    DeadCellsCoreModding build script
.DESCRIPTION
    Builds the DeadCellsCoreModding project across four stages: Native, MDK, Core, and Assets.

    Future parallel potential (for -Parallel flag):
      - Stage 1 (Native) and Stage 4 (Assets) can run in parallel (no dependencies)
      - Stage 2 (MDK) depends on Stage 1 (needs GameProxy.dll as reference)
      - Stage 3 (Core) depends on Stage 1 (needs modcorenative.dll) and Stage 2 (needs GameProxy.dll)
      - Stage 4 (Assets) is independent
.PARAMETER BuildNative
    Whether to build the Native stage (CMake + NonPublicMemberScanner + Goldberg copy)
.PARAMETER BuildMDK
    Whether to build the MDK stage (Haxe proxy generation + MDK publish)
.PARAMETER BuildCore
    Whether to build the Core stage (ModCore + Shell publish)
.PARAMETER BuildAssets
    Whether to build the Assets stage (res.pak generation)
.PARAMETER DebugBuild
    Whether to build in Debug configuration
.EXAMPLE
    .\buildWin.ps1
    .\buildWin.ps1 -DebugBuild -BuildNative:$false
#>
[CmdletBinding()]
param (
    [Parameter()]
    [ValidateNotNull()]
    [bool]$BuildNative = $true,

    [Parameter()]
    [ValidateNotNull()]
    [bool]$BuildMDK = $true,

    [Parameter()]
    [ValidateNotNull()]
    [bool]$BuildCore = $true,

    [Parameter()]
    [ValidateNotNull()]
    [bool]$BuildAssets = $true,

    [Parameter()]
    [ValidateNotNull()]
    [switch]$DebugBuild
)

# ============================================================
# Global error handling
# ============================================================
$ErrorActionPreference = 'Stop'

# ============================================================
# Centralized build configuration
# ============================================================

#$IsWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
#$IsLinux = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux)

if($IsWindows) {
    $BuildConfig = @{
        OutputDir             = 'bin/core'
        NativeOutputDir       = 'bin/core/native/win-x64'
        MDKOutputDir          = 'bin/core/mdk'
        MDKRefDir             = 'bin/core/mdk/ref'
        CMakePreset           = 'win-x64-release'
        CMakeBuildDir         = 'out/build/win-x64-release'
        BuildConf             = if ($DebugBuild) { 'Debug' } else { 'Release' }
        ExposureLib           = @('hljit.dll', 'libhl.dll')
    }
} 
elseif($IsLinux) {
    $BuildConfig = @{
        OutputDir             = 'bin/core'
        NativeOutputDir       = 'bin/core/native/linux-x64'
        MDKOutputDir          = 'bin/core/mdk'
        MDKRefDir             = 'bin/core/mdk/ref'
        CMakePreset           = 'linux-x64-release'
        CMakeBuildDir         = 'out/build/linux-x64-release'
        BuildConf             = if ($DebugBuild) { 'Debug' } else { 'Release' }
        ExposureLib           = @('libhl.so', 'hljit.so')
    }
}
else {
    throw "Unsupported OS platform. This build script only supports Windows and Linux."
}

# Build stage timing tracker
$BuildTimings = @{}

# ============================================================
# Logging helper functions
# ============================================================

function Write-BuildStep {
    <#
    .SYNOPSIS
        Output a timestamped, structured build log message.
    #>
    param([string]$Stage, [string]$Message)
    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] [$Stage] $Message" -ForegroundColor Cyan
}

function Write-BuildSuccess {
    <#
    .SYNOPSIS
        Output a success message.
    #>
    param([string]$Message)
    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] [SUCCESS] $Message" -ForegroundColor Green
}

function Write-BuildError {
    <#
    .SYNOPSIS
        Output an error message.
    #>
    param([string]$Message)
    $timestamp = Get-Date -Format 'HH:mm:ss'
    Write-Host "[$timestamp] [ERROR] $Message" -ForegroundColor Red
}

function Measure-BuildStep {
    <#
    .SYNOPSIS
        Execute a build stage and automatically measure elapsed time.
    #>
    param(
        [string]$StepName,
        [scriptblock]$ScriptBlock
    )
    Write-BuildStep $StepName 'Starting...'
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $ScriptBlock
        $sw.Stop()
        $BuildTimings[$StepName] = $sw.Elapsed
        Write-BuildSuccess "$StepName completed in $($sw.Elapsed.ToString('mm\:ss\.ff'))"
    }
    catch {
        $sw.Stop()
        Write-BuildError "$StepName failed: $_"
        throw
    }
}

# ============================================================
# Unified dotnet CLI helper
# ============================================================

function Invoke-DotNet {
    <#
    .SYNOPSIS
        Unified wrapper for all dotnet CLI calls with automatic error checking.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,

        [Parameter(Mandatory = $true)]
        [string]$Arguments,

        [string]$WorkingDir = $null,

        [switch]$NoRedirect
        )

    $dotnetCmd = "& dotnet $($Command) $($Arguments)" 
    if ($WorkingDir) { 
        Write-BuildStep 'dotnet' "Executing: dotnet $dotnetCmd (working dir: $WorkingDir)"
        Push-Location $WorkingDir
        try {
            Invoke-Expression $dotnetCmd
        }
        finally {
            Pop-Location
        }
    }
    else {
        Invoke-Expression $dotnetCmd
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $Command $Arguments failed with exit code: $LASTEXITCODE"
    }  
}

# ============================================================
# Stage 1: Native build
# ============================================================

function Invoke-NativeBuild {
    <#
    .SYNOPSIS
        Build native components (CMake + NonPublicMemberScanner + Goldberg copy).
    #>
    Write-BuildStep 'Native' 'Starting CMake configuration...'

    Push-Location "$PSScriptRoot/sources/native"
    try {
        # CMake configure
        & cmake . --preset=$($BuildConfig.CMakePreset) 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "CMake configure failed with exit code: $LASTEXITCODE"
        }

                # CMake build
        Write-BuildStep 'Native' 'Starting CMake build...'
        & cmake --build ./$($BuildConfig.CMakeBuildDir) 2>&1
       
        if ($LASTEXITCODE -ne 0) {
            throw "CMake build failed with exit code: $LASTEXITCODE"
        }
        
        # I dont know why
        & cmake . --preset=$($BuildConfig.CMakePreset) 2>&1
        & cmake --build ./$($BuildConfig.CMakeBuildDir) 2>&1
    }
    finally {
        Pop-Location
    }

    $nativedir = Join-Path $PSScriptRoot $BuildConfig.NativeOutputDir

    # Verify key native artifacts
    if($IsWindows) {
        $expectedArtifacts = @('libhl.dll', 'hljit.dll', 'modcorenative.dll', 'nethost.dll')
    }
    elseif($IsLinux) {
        $expectedArtifacts = @('libhl.so', 'hljit.so', 'modcorenative.so')
    }
    else {
        throw "Unsupported OS platform. This build script only supports Windows and Linux."
    }

    foreach ($artifact in $expectedArtifacts) {
        $artifactPath = Join-Path $nativedir $artifact
        if (-not (Test-Path $artifactPath)) {
            throw "Missing native artifact: $artifactPath"
        }
    }
    Write-BuildSuccess 'Native artifacts verified'

            # NonPublicMemberScanner
    Write-BuildStep 'Native' 'Scanning non-public members...'

    & dotnet run -c Release --no-launch-profile `
        --project "$PSScriptRoot/tools/NonPublicMemberScanner" `
        "$nativedir" $($BuildConfig.ExposureLib)  2>&1

    if ($LASTEXITCODE -ne 0) {   
        throw "NonPublicMemberScanner failed with exit code: $LASTEXITCODE"
    }    

    if($IsWindows) {
        # Copy Goldberg
        Write-BuildStep 'Native' 'Copying Goldberg library...'
        $goldbergSrc = Join-Path $PSScriptRoot '3rd/Goldberg/win-x64'
        if (-not (Test-Path $goldbergSrc)) {
            throw "Goldberg source directory not found: $goldbergSrc"
        }

        $goldbergDest = Join-Path $nativedir 'goldberg'
        if (-not (Test-Path $goldbergDest)) {
            New-Item -ItemType Directory -Path $goldbergDest -Force | Out-Null
        }

        # Use robocopy instead of Copy-Item for incremental copy and cleaner logging
        robocopy $goldbergSrc $goldbergDest /E /NP /NJH /NJS /R:3 /W:3
        # robocopy exit codes 0-7 are considered success (0=nothing copied, 1=files copied, 2=extra files in dest, 3=copied+extra)    # robocopy exit codes 0-7 are considered success (0=nothing copied, 1=files copied, 2=extra files in dest, 3=copied+extra)
        if ($LASTEXITCODE -ge 8) {
            throw "Goldberg copy failed, robocopy exit code: $LASTEXITCODE"
        }
        Write-BuildSuccess 'Goldberg copy completed'
    }
}

# ============================================================
# Stage 2: MDK build
# ============================================================

function Invoke-MDKBuild {
    <#
    .SYNOPSIS
        Build MDK (Haxe proxy generation + MDK publish).
    #>
    $mdkRefDir = Join-Path $PSScriptRoot $BuildConfig.MDKRefDir
    if (-not (Test-Path $mdkRefDir)) {
        New-Item -ItemType Directory -Path $mdkRefDir -Force | Out-Null
    }

    # Build ModCore.Native.Fody
    Write-BuildStep 'MDK' 'Building ModCore.Native.Fody...'
    Invoke-DotNet -Command 'build' -Arguments "`"$PSScriptRoot/sources/ModCore.Native.Fody`""

    # HaxeProxyGenerator
    $hlbootPath = Join-Path $PSScriptRoot 'hlboots/hlboot-opengl-steam.dat'
    if (-not (Test-Path $hlbootPath)) {
        throw "hlboot-opengl-steam.dat not found: $hlbootPath"
    }

    $proxyDllPath = Join-Path $PSScriptRoot "$($BuildConfig.MDKRefDir)/GameProxy.dll"
    Write-BuildStep 'MDK' 'Generating Haxe proxy...'
    Invoke-DotNet -Command 'run' -Arguments "-c Release --no-launch-profile --project `"$PSScriptRoot/tools/HaxeProxyGenerator`" `"$hlbootPath`" `"$proxyDllPath`""

    # Verify GameProxy.dll was generated
    if (-not (Test-Path $proxyDllPath)) {
        throw "GameProxy.dll generation failed: $proxyDllPath"
    }

    # Build MDK
    Write-BuildStep 'MDK' 'Building and publishing MDK...'
    Invoke-DotNet -Command 'publish' -Arguments "/p:Platform=`"Any CPU`" `"$PSScriptRoot/mdk`""

    $mdkDest = Join-Path $PSScriptRoot $BuildConfig.MDKOutputDir
    if (-not (Test-Path $mdkDest)) {
        New-Item -ItemType Directory -Path $mdkDest -Force | Out-Null
    }

    $mdkPublishSrc = Join-Path $PSScriptRoot 'mdk/bin'
    if (Test-Path $mdkPublishSrc) {
        Get-ChildItem -Path $mdkPublishSrc | Copy-Item -Destination $mdkDest -Force -Recurse
    }
    else {
        throw "MDK publish output directory not found: $mdkPublishSrc"
    }
}

# ============================================================
# Stage 3: Core build
# ============================================================

function Invoke-CoreBuild {
    <#
    .SYNOPSIS
        Build Core components (ModCore + Shell publish).
    #>
    $buildConf = $BuildConfig.BuildConf
    $srcDir = Join-Path $PSScriptRoot 'sources'

    # Build DCCMShell
    Write-BuildStep 'Core' 'Building DCCMShell...'
    Invoke-DotNet -Command 'build' -Arguments "-c $buildConf `"$srcDir/DCCMShell`""

    # Build ModCore.ModLoader.Default
    Write-BuildStep 'Core' 'Building ModCore.ModLoader.Default...'
    Invoke-DotNet -Command 'build' -Arguments "-c $buildConf `"$srcDir/ModCore.ModLoader.Default`""

    # Publish DeadCellsModding (NativeAOT)
    Write-BuildStep 'Core' 'Publishing DeadCellsModding (NativeAOT)...'
    Invoke-DotNet -Command 'publish' -Arguments "-c Release `"$srcDir/DeadCellsModding`""

    # Publish SteamStartShell
    Write-BuildStep 'Core' 'Publishing SteamStartShell...'
    Invoke-DotNet -Command 'publish' -Arguments "-c $buildConf `"$srcDir/SteamStartShell`""
}

# ============================================================
# Stage 4: Assets build
# ============================================================

function Invoke-AssetsBuild {
    <#
    .SYNOPSIS
        Build Assets (res.pak generation).
    #>
    $assetsProjDir = Join-Path $PSScriptRoot 'sources/ModCore.Assets'
    if (-not (Test-Path $assetsProjDir)) {
        throw "ModCore.Assets project directory not found: $assetsProjDir"
    }

    $buildConf = $BuildConfig.BuildConf
    Write-BuildStep 'Assets' 'Building ModCore.Assets...'
    Invoke-DotNet -Command 'build' -Arguments "-c $buildConf `"$assetsProjDir`""

    # Verify res.pak generation
    $resPakPath = Join-Path $PSScriptRoot "$($BuildConfig.OutputDir)/host/res.pak"
    if (-not (Test-Path $resPakPath)) {
        throw "res.pak not generated: $resPakPath"
    }
    Write-BuildSuccess "res.pak generated: $resPakPath"
}

# ============================================================
# Build summary report
# ============================================================

function Write-BuildSummary {
    <#
    .SYNOPSIS
        Output a complete build report: timing stats, artifact manifest, file sizes.
    #>
    Write-Host ''
    Write-Host '=' * 60 -ForegroundColor Cyan
    Write-Host '  Build Report' -ForegroundColor Cyan
    Write-Host '=' * 60 -ForegroundColor Cyan

    # Timing statistics
    Write-Host ''
    Write-Host '--- Timing ---' -ForegroundColor Yellow
    $totalTime = [TimeSpan]::Zero
    foreach ($key in $BuildTimings.Keys) {
        $elapsed = $BuildTimings[$key]
        $totalTime += $elapsed
        Write-Host "  $key : $($elapsed.ToString('mm\:ss\.ff'))" -ForegroundColor White
    }
    Write-Host "  Total  : $($totalTime.ToString('mm\:ss\.ff'))" -ForegroundColor Green
    Write-Host ''

    # Artifact manifest verification
    Write-Host '--- Artifacts ---' -ForegroundColor Yellow

    $outputDir = Join-Path $PSScriptRoot $BuildConfig.OutputDir

    if($IsWindows) {
        $artifacts = @(
            @{ Path = "$outputDir/host/DCCMShell.dll";                   Name = 'DCCMShell.dll' },
            @{ Path = "$outputDir/host/startup/DeadCellsModding.exe";    Name = 'DeadCellsModding.exe' },
            @{ Path = "$outputDir/host/startup/steam/deadcells.exe";     Name = 'deadcells.exe (steam)' },
            @{ Path = "$outputDir/host/res.pak";                         Name = 'res.pak' },
            @{ Path = "$outputDir/native/win-x64/libhl.dll";             Name = 'libhl.dll' },
            @{ Path = "$outputDir/native/win-x64/modcorenative.dll";     Name = 'modcorenative.dll' },
            @{ Path = "$outputDir/mdk/ref/GameProxy.dll";                Name = 'GameProxy.dll' }
        )
    }
    elseif($IsLinux) {
        $artifacts = @(
            @{ Path = "$outputDir/host/DCCMShell.dll";                   Name = 'DCCMShell.dll' },
            @{ Path = "$outputDir/host/startup/DeadCellsModding";    Name = 'DeadCellsModding' },
            @{ Path = "$outputDir/host/startup/steam/deadcells";     Name = 'deadcells (steam)' },
            @{ Path = "$outputDir/host/res.pak";                         Name = 'res.pak' },
            @{ Path = "$outputDir/native/linux-x64/libhl.so";             Name = 'libhl.so' },
            @{ Path = "$outputDir/native/linux-x64/modcorenative.so";     Name = 'modcorenative.so' },
            @{ Path = "$outputDir/mdk/ref/GameProxy.dll";                Name = 'GameProxy.dll' }
        )
    }

    $allPresent = $true
    foreach ($artifact in $artifacts) {
        $path = $artifact.Path
        $name = $artifact.Name
        if (Test-Path $path) {
            $fileInfo = Get-Item $path
            $sizeKB = [math]::Round($fileInfo.Length / 1KB, 2)
            Write-Host "  [OK] $name ($sizeKB KB)" -ForegroundColor Green
        }
        else {
            Write-Host "  [MISSING] $name" -ForegroundColor Red
            $allPresent = $false
        }
    }

    Write-Host ''

    # Write report file
    $reportDir = Join-Path $PSScriptRoot $BuildConfig.OutputDir
    if (-not (Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }

    $reportName = 'BuildReport_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.txt'
    $reportPath = Join-Path $reportDir $reportName

    $reportContent = @"
DeadCellsCoreModding Build Report
=================================
Build time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Configuration: $($BuildConfig.BuildConf)

--- Timing ---
$($BuildTimings.Keys | ForEach-Object { "  $_ : $($BuildTimings[$_].ToString('mm\:ss\.ff'))" } | Out-String)
Total: $($totalTime.ToString('mm\:ss\.ff'))

--- Artifacts ---
$($artifacts | ForEach-Object {
    if (Test-Path $_.Path) {
        $info = Get-Item $_.Path
        "[OK] $($_.Name) ($([math]::Round($info.Length / 1KB, 2)) KB)"
    } else {
        "[MISSING] $($_.Name)"
    }
} | Out-String)
"@

    # Return exit code
    if (-not $allPresent) {
        Write-BuildError 'Some artifacts are missing; build is incomplete.'
        exit 1
    }

    Write-BuildSuccess 'All artifacts present; build succeeded!'
    exit 0
}

# ============================================================
# Main build flow
# ============================================================

# Change to script directory
Set-Location $PSScriptRoot

try {
    $buildSummary = $true

    # Stage 1: Native
    if ($BuildNative) {
        Measure-BuildStep 'Native' { Invoke-NativeBuild }
    } else {
        $buildSummary = $false
    }

    # Stage 2: MDK
    if ($BuildMDK) {
        Measure-BuildStep 'MDK' { Invoke-MDKBuild }
    } else {
        $buildSummary = $false
    }

    # Stage 3: Core
    if ($BuildCore) {
        Measure-BuildStep 'Core' { Invoke-CoreBuild }
    } else {
        $buildSummary = $false
    }

    # Stage 4: Assets
    if ($BuildAssets) {
        Measure-BuildStep 'Assets' { Invoke-AssetsBuild }
    } else {
        $buildSummary = $false
    }

    # Build summary report
    if($buildSummary) {
        Write-BuildSummary
    }
}
catch {
    Write-BuildError "Build failed: $_"
    exit 1
}
