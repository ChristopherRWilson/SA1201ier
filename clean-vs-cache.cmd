@echo off
REM Clean Visual Studio and build caches
echo Cleaning Visual Studio and build caches...

REM Remove Visual Studio cache
if exist .vs rd /s /q .vs
echo Removed .vs folder

REM Remove obj and bin folders from all projects
for /d /r %%d in (obj) do @if exist "%%d" rd /s /q "%%d"
for /d /r %%d in (bin) do @if exist "%%d" rd /s /q "%%d"
echo Removed obj and bin folders

REM Restore packages
echo Restoring NuGet packages...
dotnet restore

echo.
echo Done! You can now open the solution in Visual Studio.
pause
