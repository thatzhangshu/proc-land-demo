@echo off
set "LANDMASS_EDITOR=C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe"
if not exist "%LANDMASS_EDITOR%" (
  echo Unity 2022.3.51f1 was not found. Open this folder from Unity Hub.
  pause
  exit /b 1
)
start "" "%LANDMASS_EDITOR%" -projectPath "%~dp0."
