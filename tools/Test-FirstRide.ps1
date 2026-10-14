param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.com',
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$rideProject = Join-Path (Split-Path $PSScriptRoot -Parent) 'UnityGame'
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity 6.5 not found: $UnityEditor" }
if ($rideProject.Length -gt 100) { Write-Warning 'Use a short checkout path such as C:\Work\YTSuper8 to avoid Windows/Mono package-import path limits.' }
& $UnityEditor -batchmode -force-d3d11 -projectPath $rideProject `
    -executeMethod FirstRide.Editor.FirstRideProject.PlaySmoke -logFile (Join-Path $rideProject 'playmode.log')
if ($LASTEXITCODE -ne 0) { throw "Play Mode validation failed. See $rideProject\playmode.log" }
Get-Content -LiteralPath (Join-Path $rideProject 'TestResults\playmode.txt')
if ($Build) {
    & $UnityEditor -batchmode -force-d3d11 -quit -projectPath $rideProject `
        -executeMethod FirstRide.Editor.FirstRideProject.BuildWindows -logFile (Join-Path $rideProject 'build.log')
    if ($LASTEXITCODE -ne 0) { throw "Windows build failed. See $rideProject\build.log" }
    Write-Output (Join-Path $rideProject 'Builds\Windows\RollerCoasterHeroes.exe')
}
