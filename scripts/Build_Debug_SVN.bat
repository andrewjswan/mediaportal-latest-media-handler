@echo off
cls
Title Building MediaPortal Latest Media Handler (RELEASE)
cd ..

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
	:: 64-bit
	set PROGS=%programfiles(x86)%
	goto CONT
:32BIT
	set PROGS=%ProgramFiles%
:CONT

:: Prepare version
subwcrev . LatestMediaHandler\Properties\AssemblyInfo.cs LatestMediaHandler\Properties\AssemblyInfo.cs

:: Build
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBUILD.exe" /target:Rebuild /property:Configuration=DEBUG /fl /flp:logfile=LatestMediaHandler.log;verbosity=diagnostic LatestMediaHandler.sln

:: Revert version
svn revert LatestMediaHandler\Properties\AssemblyInfo.cs

cd scripts

pause

