@echo off
rem IRB is used so we can get output, otherwise we can't see the output in our .NET unit tests
SET IRB_PATH=%1
SHIFT
set after1=

:loop
if "%1" == "" goto end
set after1=%after1% %1
SHIFT
goto loop

:end
irb %IRB_PATH% start -R config.ru -p 8132 -V %after1%
