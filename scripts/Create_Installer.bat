@echo off
cls
Title Creating MediaPortal Latest Media Handler Installer

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT

IF NOT EXIST "%PROGS%\Team MediaPortal\MediaPortal\" SET PROGS=C:

:: Get version from DLL
FOR /F "tokens=*" %%i IN ('Tools\sigcheck.exe /accepteula /nobanner /n "..\LatestMediaHandler\bin\Release\LatestMediaHandler.dll"') DO (SET version=%%i)

:: Temp xmp2 file
copy /Y LatestMediaHandler.xmp2 LatestMediaHandlerTemp.xmp2

:: Build mpe1
"%PROGS%\Team MediaPortal\MediaPortal\MPEMaker.exe" LatestMediaHandlerTemp.xmp2 /B /V=%version% /UpdateXML

:: Cleanup
del LatestMediaHandlerTemp.xmp2
