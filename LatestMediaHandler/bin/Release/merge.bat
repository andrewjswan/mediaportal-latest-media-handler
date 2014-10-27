@echo off

if "%programfiles(x86)%XXX"=="XXX" goto 32BIT
    :: 64-bit
    set PROGS=%programfiles(x86)%
    rem pause
    rem echo Current path is %PROGS%	
    goto CONT
:32BIT
    set PROGS=%ProgramFiles%
    rem echo Current path is %PROGS%
    rem pause
:CONT

IF EXIST LatestMediaHandler_UNMERGED.dll del LatestMediaHandler_UNMERGED.dll
ren LatestMediaHandler.dll LatestMediaHandler_UNMERGED.dll
ilmerge /out:LatestMediaHandler.dll LatestMediaHandler_UNMERGED.dll NLog.dll /targetplatform:"v4,%PROGS%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" /wildcards
