@echo off
cls
Title Creating MediaPortal Latest Media Handler Installer

:: Check for modification
svn status ..\source | findstr "^M"
if ERRORLEVEL 1 (
	echo No modifications in source folder.
) else (
	echo There are modifications in source folder. Aborting.
	pause
	exit 1
)

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT

:: Get version from DLL
FOR /F "tokens=1-3" %%i IN ('Tools\sigcheck.exe "..\LatestMediaHandler\bin\Release\LatestMediaHandler.dll"') DO ( IF "%%i %%j"=="File version:" SET version=%%k )

:: trim version
SET version=%version:~0,-1%

:: Temp xmp2 file
copy LatestMediaHandler.xmp2 LatestMediaHandlerTemp.xmp2

:: Sed "LatestMediaHandler-{VERSION}.xml" from xmp2 file
Tools\sed.exe -i "s/LatestMediaHandler-{VERSION}.xml/LatestMediaHandler-%version%.xml/g" LatestMediaHandlerTemp.xmp2

:: Build mpe1
"%PROGS%\Team MediaPortal\MediaPortal\MPEMaker.exe" LatestMediaHandlerTemp.xmp2 /B /V=%version% /UpdateXML

:: Cleanup
del LatestMediaHandlerTemp.xmp2

:: Sed "LatestMediaHandler-{VERSION}.mpe1" from LatestMediaHandler.xml
Tools\sed.exe -i "s/LatestMediaHandler-{VERSION}.mpe1/LatestMediaHandler-%version%.mpe1/g" LatestMediaHandler-%version%.xml

:: Parse version (Might be needed in the futute)
FOR /F "tokens=1-4 delims=." %%i IN ("%version%") DO ( 
	SET major=%%i
	SET minor=%%j
	SET build=%%k
	SET revision=%%l
)

:: Rename mpe1
if exist "..\builds\LatestMediaHandler-%major%.%minor%.%build%.%revision%.mpe1" del "..\builds\LatestMediaHandler-%major%.%minor%.%build%.%revision%.mpe1"
rename ..\builds\LatestMediaHandler-MAJOR.MINOR.BUILD.REVISION.mpe1 "LatestMediaHandler-%major%.%minor%.%build%.%revision%.mpe1"


