@echo off
setlocal enabledelayedexpansion

set CmdDir=%~dp0 
set CmdDir=%CmdDir:~0,-1%
set symlink=%1\%2

if exist %3% (
	echo ----Nuget repo already exists at %3----
)
if not exist %3% (
	echo ----Creating NuGet repo for first time at %3%----
	mkdir %3%
	if errorlevel 1 goto nodir
)

set nugetfile=%1%\nuget.config

if exist %nugetfile% (
	echo ----Nuget Add Package Config File already exists at %nugetfile%----
)
if not exist %nugetfile% (
	echo ----Copied NuGet Add Package Config File to %nugetfile% ensure relative paths----
	copy %CmdDir%\nuget.parent.config %nugetfile%
	if errorlevel 1 goto noconfig
)

if exist %symlink% (
	echo ----NuGet symlink already created at %symlink%----
	GOTO END
)

echo ----Creating symlink at %symlink%----

cd %1
mklink /J %2 %3
if errorlevel 1 goto nosymlink

:END
exit /B 0
:nodir
echo Unable to create repo directory at %3%
exit /B 1
:noconfig
echo Unable to copy %CmdDir%\nuget.parent.config to %nugetfile%
exit /B 1

:nosymlink
echo Unable to create symlink from %symlink% to %3
