param(
    [string]$OutputMsi = "FolderArchiveTool.msi",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$proj = Join-Path -Path $scriptRoot -ChildPath "..\FolderArchiveTool\FolderArchiveTool.csproj"
$publishDir = Join-Path $scriptRoot "publish"

if (-not (Test-Path $proj)) { Write-Error "Project not found: $proj"; exit 1 }

Write-Host "Publishing project to $publishDir..."
dotnet publish $proj -c $Configuration -r $Runtime --self-contained false -o $publishDir --no-restore
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit $LASTEXITCODE }

# Ensure WiX tools are available
$heat = "heat.exe"
$candle = "candle.exe"
$light = "light.exe"

if (-not (Get-Command $heat -ErrorAction SilentlyContinue)) { Write-Error "heat.exe not found in PATH. Install WiX Toolset (https://wixtoolset.org/) or run this script in CI after installing WiX."; exit 1 }
if (-not (Get-Command $candle -ErrorAction SilentlyContinue)) { Write-Error "candle.exe not found in PATH."; exit 1 }
if (-not (Get-Command $light -ErrorAction SilentlyContinue)) { Write-Error "light.exe not found in PATH."; exit 1 }

# Prepare intermediate output
$harvestWxs = Join-Path $scriptRoot "AppFiles.wxs"
$prodWxs = Join-Path $scriptRoot "Product.wxs"
$objDir = Join-Path $scriptRoot "obj"
New-Item -ItemType Directory -Force -Path $objDir | Out-Null

# Use heat to harvest the publish folder into a component group named AppFiles
Write-Host "Harvesting files with heat: $publishDir -> $harvestWxs"
& $heat dir "$publishDir" -cg AppFiles -dr INSTALLFOLDER -scom -sreg -gg -g1 -sfrag -out "$harvestWxs"
if ($LASTEXITCODE -ne 0) { Write-Error "heat.exe failed"; exit $LASTEXITCODE }

# Compile .wxs to .wixobj
Write-Host "Compiling WiX sources with candle"
& $candle -out (Join-Path $objDir "AppFiles.wixobj") "$harvestWxs" "$prodWxs" 2>&1 | Write-Host
if ($LASTEXITCODE -ne 0) { Write-Error "candle.exe failed"; exit $LASTEXITCODE }

# Link to MSI
$msiOut = Join-Path (Split-Path -Parent $scriptRoot) $OutputMsi
Write-Host "Linking to create MSI: $msiOut"
& $light -out "$msiOut" (Join-Path $objDir "AppFiles.wixobj") -ext WixUIExtension 2>&1 | Write-Host
if ($LASTEXITCODE -ne 0) { Write-Error "light.exe failed"; exit $LASTEXITCODE }

Write-Host "MSI created: $msiOut"
return 0