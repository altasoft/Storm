@echo off
setlocal

REM Check if a version parameter was provided
if "%1"=="" (
    echo Error: Version parameter is required.
    echo Usage: build_and_pack.bat [version]
    exit /b 1
)

REM Set the version parameter
set VERSION=%1

echo Updating SharedAssemblyInfo.props version to %VERSION%...
REM Use PowerShell to update the version in SharedAssemblyInfo.props
powershell -NoProfile -ExecutionPolicy Bypass -Command "(Get-Content 'src\SharedAssemblyInfo.props') -replace '<Version>.*</Version>', '<Version>%VERSION%</Version>' | Set-Content 'src\SharedAssemblyInfo.props'"
if %errorlevel% neq 0 (
    echo Failed to update version in SharedAssemblyInfo.props.
    exit /b %errorlevel%
)

echo Updating build\AltaSoft.Storm.MsSql.targets version to %VERSION%...
REM Use PowerShell to update the version in build\AltaSoft.Storm.MsSql.targets
PowerShell -NoProfile -ExecutionPolicy Bypass -File "Update-TaskAssemblyVersion.ps1" -NewVersion "%VERSION%" -FilePath "src\AltaSoft.Storm.Weaver\build\AltaSoft.Storm.Generator.MsSql.targets"
if %errorlevel% neq 0 (
    echo Failed to update version in build\AltaSoft.Storm.MsSql.targets
    exit /b %errorlevel%
)

echo Updating buildMultiTargeting\AltaSoft.Storm.MsSql.targets version to %VERSION%...
REM Use PowerShell to update the version in buildMultiTargeting\AltaSoft.Storm.MsSql.targets
PowerShell -NoProfile -ExecutionPolicy Bypass -File "Update-TaskAssemblyVersion.ps1" -NewVersion "%VERSION%" -FilePath "src\AltaSoft.Storm.Weaver\buildMultiTargeting\AltaSoft.Storm.Generator.MsSql.targets"
if %errorlevel% neq 0 (
    echo Failed to update version in buildMultiTargeting\AltaSoft.Storm.MsSql.targets
    exit /b %errorlevel%
)


dotnet restore src/AltaSoft.Storm.Generator.Common/
dotnet restore src/AltaSoft.Storm.Weaver/
dotnet restore src/AltaSoft.Storm.Generator/
dotnet restore src/AltaSoft.Storm/
dotnet restore src/AltaSoft.Storm.MsSql/
dotnet restore src/AltaSoft.Storm.Analyzers/

dotnet build -c Release src/AltaSoft.Storm.Weaver/ --no-restore
dotnet build -c Release src/AltaSoft.Storm/ --no-restore
dotnet build -c Release src/AltaSoft.Storm.MsSql/ --no-restore
dotnet build -c Release src/AltaSoft.Storm.Analyzers/ --no-restore
dotnet build -c Release src/AltaSoft.Storm.Generator/ --no-restore

dotnet pack -c Release -o ./nupkgs src/AltaSoft.Storm.Generator/ --no-build
dotnet pack -c Release -o ./nupkgs src/AltaSoft.Storm.MsSql/ --no-build
