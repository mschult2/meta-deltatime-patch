# This script builds C files (.c) into an Android library, aka Unity plugin (.so)

function Require-Paths {
    param(
        [Parameter(Mandatory=$true)]
        [array] $Items   # Each item: @{ Path = "..."; Label = "..." }
    )

    foreach ($item in $Items) {
        $path  = $item.Path
        $label = $item.Label
		$varName = $item.VarName

        if (-not (Test-Path $path)) {
            Write-Host "ERROR: $label not found at $path" -ForegroundColor Red
            Write-Host "Please open build.ps1 and edit `$$varName." -ForegroundColor Red
            return $false
        }
    }

    return $true
}

try
{
	# Change directory if invoked externally
	Push-Location
	$BUILD_DIR = "$PSScriptRoot/build-android"
	New-Item -ItemType Directory -Path $BUILD_DIR -Force
	Set-Location $BUILD_DIR

	# Paths
	$ANDROID_NDK = "C:\Program Files\Unity\Hub\Editor\6000.0.59f2\Editor\Data\PlaybackEngines\AndroidPlayer\NDK"
	$CMAKE_DIR   = "C:\Program Files\Unity\Hub\Editor\6000.0.59f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmake\3.22.1\bin"
	$CMAKE_EXE   = "$CMAKE_DIR\cmake.exe"
	$NINJA_EXE   = "$CMAKE_DIR\ninja.exe"
	
	if (-not (Require-Paths @(
		@{ Path = $ANDROID_NDK; Label = "Android NDK"; VarName = "ANDROID_NDK" }
		@{ Path = $CMAKE_DIR;   Label = "CMake folder"; VarName = "CMAKE_DIR" }
		@{ Path = $CMAKE_EXE;   Label = "cmake.exe"; VarName = "CMAKE_EXE" }
		@{ Path = $NINJA_EXE;   Label = "ninja.exe"; VarName = "NINJA_EXE" }
	))) {
		return
	}

	Write-Host "Configuring..."
	& $CMAKE_EXE -G "Ninja" `
		-D CMAKE_MAKE_PROGRAM="$NINJA_EXE" `
		-D CMAKE_TOOLCHAIN_FILE="$ANDROID_NDK\build\cmake\android.toolchain.cmake" `
		-D ANDROID_ABI=arm64-v8a `
		-D ANDROID_PLATFORM=android-24 `
		..

	Write-Host "Building..."
	& $CMAKE_EXE --build . --config Release

	Write-Host
	Write-Host "Build succeeded. The resulting .so file may be found in $BUILD_DIR"
}
catch
{
	Write-Error $_
}
finally
{
    # Restore the original directory
    Pop-Location
}