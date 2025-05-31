@echo off
REM Neo build script with plugin integration v1.1.0
REM This script builds Neo.CLI and all plugins, organizing them in framework-specific directories

setlocal enabledelayedexpansion

REM Start time measurement
for /f "tokens=1-4 delims=:.," %%a in ("%time%") do (
   set /A "start=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)

echo Neo Build Script with Plugin Integration
echo.

REM Set the directory variables
set "SCRIPT_DIR=%~dp0"
set "ROOT_DIR=%SCRIPT_DIR%.."
set "BUILD_CONFIG=Release"
set "OUTPUT_DIR=%ROOT_DIR%\bin"
set "CLI_PROJECT=%ROOT_DIR%\src\Neo.CLI\Neo.CLI.csproj"
set "NEO_SOLUTION=%ROOT_DIR%\neo.sln"
set "PLUGINS_FOUND=0"
set "PLUGINS_PROCESSED=0"

REM Log file setup
set "LOG_FILE=%ROOT_DIR%\build.log"
echo Build started at %date% %time% > "%LOG_FILE%"

REM Extract target framework from Neo.CLI.csproj
echo Detecting .NET version from Neo.CLI project...
echo Detecting .NET version from Neo.CLI project... >> "%LOG_FILE%"

if not exist "%CLI_PROJECT%" (
    echo Error: Could not find Neo.CLI project at %CLI_PROJECT%
    echo Error: Could not find Neo.CLI project at %CLI_PROJECT% >> "%LOG_FILE%"
    exit /b 1
)

REM Find the TargetFramework in the project file
for /f "tokens=2 delims=<>" %%a in ('findstr /C:"<TargetFramework>" "%CLI_PROJECT%"') do (
    set "TARGET_FRAMEWORK=%%a"
)

if not defined TARGET_FRAMEWORK (
    echo Error: Could not detect target framework from Neo.CLI project
    echo Falling back to default: net7.0
    echo Warning: Could not detect target framework, falling back to net7.0 >> "%LOG_FILE%"
    set "TARGET_FRAMEWORK=net7.0"
) else (
    echo Detected target framework: %TARGET_FRAMEWORK%
    echo Detected target framework: %TARGET_FRAMEWORK% >> "%LOG_FILE%"
)

echo Root directory: %ROOT_DIR%
echo Output directory: %OUTPUT_DIR%
echo Target framework: %TARGET_FRAMEWORK%
echo.

REM Verify .NET SDK is installed
echo Verifying .NET SDK installation...
echo Verifying .NET SDK installation... >> "%LOG_FILE%"
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Error: .NET SDK is not installed or not in PATH >> "%LOG_FILE%"
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set "DOTNET_VERSION=%%v"
echo Found .NET SDK version: %DOTNET_VERSION%
echo Found .NET SDK version: %DOTNET_VERSION% >> "%LOG_FILE%"
echo.

REM Clean up old build artifacts
echo Cleaning previous build artifacts...
echo Cleaning previous build artifacts... >> "%LOG_FILE%"
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%" 2>nul
echo Clean complete
echo Clean complete >> "%LOG_FILE%"
echo.

REM First build the entire solution to ensure dependencies are built
if exist "%NEO_SOLUTION%" (
    echo Building entire Neo solution first to resolve dependencies...
    echo Building entire Neo solution first to resolve dependencies... >> "%LOG_FILE%"
    dotnet build "%NEO_SOLUTION%" -c %BUILD_CONFIG%
    if %ERRORLEVEL% neq 0 (
        echo Failed to build Neo solution
        echo Failed to build Neo solution >> "%LOG_FILE%"
        exit /b 1
    )
    echo Successfully built Neo solution
    echo Successfully built Neo solution >> "%LOG_FILE%"
    echo.
) else (
    echo Neo solution file not found. Will attempt to build Neo.CLI directly.
    echo Neo solution file not found. Will attempt to build Neo.CLI directly. >> "%LOG_FILE%"
)

REM Build Neo.CLI
echo Building project: Neo.CLI
echo Building project: Neo.CLI >> "%LOG_FILE%"
dotnet build "%CLI_PROJECT%" -c %BUILD_CONFIG%
if %ERRORLEVEL% neq 0 (
    echo Failed to build Neo.CLI
    echo Failed to build Neo.CLI >> "%LOG_FILE%"
    exit /b 1
)
echo Successfully built Neo.CLI
echo Successfully built Neo.CLI >> "%LOG_FILE%"
echo.

REM Copy Neo.CLI output to bin directory
echo Copying Neo.CLI output to bin directory...
echo Copying Neo.CLI output to bin directory... >> "%LOG_FILE%"

REM Find the Neo.CLI DLL
call :FindDll "neo-cli.dll"
set "CLI_DLL=%FOUND_DLL%"

if defined CLI_DLL if exist "!CLI_DLL!" (
    for %%F in ("!CLI_DLL!") do set "CLI_DIR=%%~dpF"
    echo Found neo-cli.dll at: !CLI_DIR!
    echo Found neo-cli.dll at: !CLI_DIR! >> "%LOG_FILE%"
    xcopy /E /Y "!CLI_DIR!*" "%OUTPUT_DIR%\"
    echo Neo.CLI copied to output directory
    echo Neo.CLI copied to output directory >> "%LOG_FILE%"
) else (
    echo Error: Could not find neo-cli.dll
    echo Trying to find any DLLs in the output directories...
    echo Warning: Could not find neo-cli.dll, searching for any DLLs >> "%LOG_FILE%"

    REM Try to find any DLL in the build output directories
    for /r "%ROOT_DIR%" %%F in (*.dll) do (
        set "ANY_DLL=%%F"
        if "!ANY_DLL!" neq "" (
            for %%G in ("!ANY_DLL!") do set "ANY_DIR=%%~dpG"
            echo Found DLLs at: !ANY_DIR!
            echo Found DLLs at: !ANY_DIR! >> "%LOG_FILE%"
            xcopy /E /Y "!ANY_DIR!*" "%OUTPUT_DIR%\"
            echo Neo.CLI copied from found directory
            echo Neo.CLI copied from found directory >> "%LOG_FILE%"
            goto :CliCopyDone
        )
    )

    echo Failed to locate any build output. Make sure the build succeeded.
    echo Error: Failed to locate any build output >> "%LOG_FILE%"
    exit /b 1
)

:CliCopyDone
echo.

REM Create plugins directory in the Neo.CLI output directory
echo Setting up plugins directory...
echo Setting up plugins directory... >> "%LOG_FILE%"

REM Create the specific Neo.CLI output directory structure
set "CLI_PLUGINS_DIR=%OUTPUT_DIR%\Neo.CLI\%TARGET_FRAMEWORK%\Plugins"
mkdir "%CLI_PLUGINS_DIR%" 2>nul

echo Plugins directory created at: %CLI_PLUGINS_DIR%
echo Plugins directory created at: %CLI_PLUGINS_DIR% >> "%LOG_FILE%"
echo.

REM Build and copy all plugins
echo Building and installing plugins...
echo Building and installing plugins... >> "%LOG_FILE%"
echo.

REM Find all plugin directories
for /d %%D in ("%ROOT_DIR%\src\Plugins\*") do (
    set "PLUGIN_DIR=%%D"
    set "PLUGIN_DIR_NAME=%%~nxD"

    REM Skip obj directory
    if "!PLUGIN_DIR_NAME!" == "obj" (
        echo Skipping !PLUGIN_DIR_NAME! - not a plugin directory
        echo Skipping !PLUGIN_DIR_NAME! - not a plugin directory >> "%LOG_FILE%"
    ) else (
        call :ProcessPluginDirectory "!PLUGIN_DIR!"
    )
)

echo All plugins have been built and installed
echo Found %PLUGINS_FOUND% plugin directories, processed %PLUGINS_PROCESSED% plugin projects
echo All plugins have been built and installed >> "%LOG_FILE%"
echo Found %PLUGINS_FOUND% plugin directories, processed %PLUGINS_PROCESSED% plugin projects >> "%LOG_FILE%"
echo.

REM Calculate build time
for /f "tokens=1-4 delims=:.," %%a in ("%time%") do (
   set /A "end=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)
set /A elapsed=end-start
set /A hh=elapsed/(60*60*100), rest=elapsed%%(60*60*100), mm=rest/(60*100), rest%%=60*100, ss=rest/100, cc=rest%%100
if %hh% lss 10 set hh=0%hh%
if %mm% lss 10 set mm=0%mm%
if %ss% lss 10 set ss=0%ss%
if %cc% lss 10 set cc=0%cc%
set DURATION=%hh%:%mm%:%ss%.%cc%

echo Build completed successfully in %DURATION%!
echo Build completed successfully in %DURATION%! >> "%LOG_FILE%"
echo.

echo Build completed successfully!
echo The Neo node with plugins is available at: %OUTPUT_DIR%\Neo.CLI\%TARGET_FRAMEWORK%
echo You can run it using: cd %OUTPUT_DIR%\Neo.CLI\%TARGET_FRAMEWORK% ^&^& dotnet neo-cli.dll

REM Verify essential plugins were installed
set "ESSENTIAL_PLUGINS=DBFTPlugin ApplicationLogs RpcServer"
for %%P in (%ESSENTIAL_PLUGINS%) do (
    if not exist "%CLI_PLUGINS_DIR%\%%P\%%P.dll" (
        echo Warning: Essential plugin %%P is missing
        echo Warning: Essential plugin %%P is missing >> "%LOG_FILE%"
    )
)

exit /b 0

:FindDll
setlocal
set "DLL_NAME=%~1"

REM First check if the DLL is already in the main bin directory
if exist "%OUTPUT_DIR%\%DLL_NAME%" (
    endlocal & set "FOUND_DLL=%OUTPUT_DIR%\%DLL_NAME%"
    exit /b 0
)

REM Check in direct project folders under bin
for /d %%D in ("%OUTPUT_DIR%\*") do (
    set "PROJECT_DIR=%%D"
    set "PROJECT_DIR_NAME=%%~nxD"

    if exist "!PROJECT_DIR!\%DLL_NAME%" (
        endlocal & set "FOUND_DLL=!PROJECT_DIR!\%DLL_NAME%"
        exit /b 0
    )

    REM Also check for nested target framework folders
    for /d %%F in ("!PROJECT_DIR!\*") do (
        if exist "%%F\%DLL_NAME%" (
            endlocal & set "FOUND_DLL=%%F\%DLL_NAME%"
            exit /b 0
        )
    )
)

REM Common locations to check in the src directory
set "SRC_DIR=%ROOT_DIR%\src"
set "PATTERNS=*\bin\%BUILD_CONFIG%\%TARGET_FRAMEWORK%\%DLL_NAME% *\bin\%BUILD_CONFIG%\%DLL_NAME% *\*\bin\%BUILD_CONFIG%\%TARGET_FRAMEWORK%\%DLL_NAME% *\*\bin\%BUILD_CONFIG%\%DLL_NAME%"

REM Check each pattern
for %%P in (%PATTERNS%) do (
    for /r "%SRC_DIR%" %%F in (%%P) do (
        if exist "%%F" (
            endlocal & set "FOUND_DLL=%%F"
            exit /b 0
        )
    )
)

REM As a last resort, search the entire src directory
echo Searching for %DLL_NAME% in src directory...
for /r "%SRC_DIR%" %%F in (%DLL_NAME%) do (
    if exist "%%F" (
        endlocal & set "FOUND_DLL=%%F"
        exit /b 0
    )
)

endlocal & set "FOUND_DLL="
exit /b 0

:ProcessPluginDirectory
setlocal EnableDelayedExpansion
set "PLUGIN_DIR=%~1"
set /a PLUGINS_FOUND+=1

REM Find all .csproj files in the directory
set "FOUND_PROJECTS=0"
for %%P in ("%PLUGIN_DIR%\*.csproj") do (
    set "PROJECT_FILE=%%P"
    set "PROJECT_NAME=%%~nP"

    REM Build the plugin
    echo Building project: !PROJECT_NAME!
    echo Building project: !PROJECT_NAME! >> "%LOG_FILE%"
    dotnet build "!PROJECT_FILE!" -c %BUILD_CONFIG%
    if %ERRORLEVEL% neq 0 (
        echo Failed to build !PROJECT_NAME!
        echo Failed to build !PROJECT_NAME! >> "%LOG_FILE%"
        exit /b 1
    )
    echo Successfully built !PROJECT_NAME!
    echo Successfully built !PROJECT_NAME! >> "%LOG_FILE%"

    REM Find the DLL file
    call :FindDll "!PROJECT_NAME!.dll"
    set "PLUGIN_DLL=%FOUND_DLL%"

    REM Create a plugin-specific folder in the Neo.CLI's plugins directory
    set "PLUGIN_SPECIFIC_DIR=%CLI_PLUGINS_DIR%\!PROJECT_NAME!"
    mkdir "%PLUGIN_SPECIFIC_DIR%" 2>nul

    REM Copy the plugin DLL to its specific directory
    if defined PLUGIN_DLL if exist "!PLUGIN_DLL!" (
        echo Copying !PROJECT_NAME!.dll to %PLUGIN_SPECIFIC_DIR%\ from !PLUGIN_DLL!
        echo Copying !PROJECT_NAME!.dll to %PLUGIN_SPECIFIC_DIR%\ from !PLUGIN_DLL! >> "%LOG_FILE%"
        copy /Y "!PLUGIN_DLL!" "%PLUGIN_SPECIFIC_DIR%\"

        REM Verify the copy was successful
        if not exist "%PLUGIN_SPECIFIC_DIR%\!PROJECT_NAME!.dll" (
            echo Error: Failed to copy !PROJECT_NAME!.dll to %PLUGIN_SPECIFIC_DIR%
            echo Error: Failed to copy !PROJECT_NAME!.dll to %PLUGIN_SPECIFIC_DIR% >> "%LOG_FILE%"
            exit /b 1
        )
    ) else (
        echo Warning: Could not find DLL for !PROJECT_NAME!
        echo Warning: Could not find DLL for !PROJECT_NAME! >> "%LOG_FILE%"
    )

    REM Look for the config file in multiple locations
    set "PLUGIN_CONFIG=%PLUGIN_DIR%\!PROJECT_NAME!.json"
    if not exist "!PLUGIN_CONFIG!" (
        REM Try to find the config elsewhere
        for /r "%PLUGIN_DIR%" %%F in (!PROJECT_NAME!.json) do (
            set "PLUGIN_CONFIG=%%F"
            goto :FoundConfig
        )
    )

    :FoundConfig
    REM Copy the plugin config if it exists
    if exist "!PLUGIN_CONFIG!" (
        echo Copying !PROJECT_NAME!.json to %PLUGIN_SPECIFIC_DIR%\ from !PLUGIN_CONFIG!
        echo Copying !PROJECT_NAME!.json to %PLUGIN_SPECIFIC_DIR%\ from !PLUGIN_CONFIG! >> "%LOG_FILE%"
        copy /Y "!PLUGIN_CONFIG!" "%PLUGIN_SPECIFIC_DIR%\"

        REM Verify the copy was successful
        if not exist "%PLUGIN_SPECIFIC_DIR%\!PROJECT_NAME!.json" (
            echo Error: Failed to copy !PROJECT_NAME!.json to %PLUGIN_SPECIFIC_DIR%
            echo Error: Failed to copy !PROJECT_NAME!.json to %PLUGIN_SPECIFIC_DIR% >> "%LOG_FILE%"
            exit /b 1
        )
    ) else (
        echo No config file found for !PROJECT_NAME!, skipping config copy
        echo No config file found for !PROJECT_NAME!, skipping config copy >> "%LOG_FILE%"
    )

    echo Plugin !PROJECT_NAME! installed
    echo Plugin !PROJECT_NAME! installed >> "%LOG_FILE%"
    echo.

    set /a FOUND_PROJECTS+=1
    set /a PLUGINS_PROCESSED+=1
)

if !FOUND_PROJECTS! equ 0 (
    echo No project files found in !PLUGIN_DIR!, skipping
    echo No project files found in !PLUGIN_DIR!, skipping >> "%LOG_FILE%"
)

endlocal & set /a PLUGINS_PROCESSED=%PLUGINS_PROCESSED% & set /a PLUGINS_FOUND=%PLUGINS_FOUND%
exit /b 0
