$ErrorActionPreference = "Stop"

$fx = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $fx "csc.exe"

if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc"
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outExe = Join-Path $root "ScreenProfileSwitcher.exe"

$sources = @(
    (Join-Path $root "src\Program.cs"),
    (Join-Path $root "src\MainWindow.cs"),
    (Join-Path $root "src\DisplayManager.cs"),
    (Join-Path $root "src\ProfileStore.cs")
)

$refs = @(
    "$fx\WPF\PresentationFramework.dll",
    "$fx\WPF\PresentationCore.dll",
    "$fx\WPF\WindowsBase.dll",
    "$fx\System.Xaml.dll",
    "$fx\System.Runtime.Serialization.dll",
    "$fx\System.Xml.dll"
)

$args = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/win32manifest:$(Join-Path $root 'app.manifest')",
    "/win32icon:$(Join-Path $root 'assets\app.ico')",
    "/resource:$(Join-Path $root 'assets\logo.png'),ScreenProfileSwitcher.logo.png",
    "/out:$outExe"
) + ($refs | ForEach-Object { "/reference:$_" }) + $sources

& $csc @args
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built successfully: $outExe"
