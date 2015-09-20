@echo off
cls
Title Deploying MediaPortal Latest Media Handler (RELEASE)
cd ..

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT

copy /y "LatestMediaHandler\bin\Release\LatestMediaHandler.dll" "%PROGS%\Team MediaPortal\MediaPortal\plugins\process\"

cd scripts