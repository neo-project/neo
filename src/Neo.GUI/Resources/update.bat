@echo off
set "taskname=neo-gui.exe"
echo waiting...
:wait
ping 127.0.1 -n 3 >nul
tasklist | find "%taskname%" /i >nul 2>nul
if "%errorlevel%" NEQ "1" goto wait
echo updating...
copy /Y update\* *
rmdir /S /Q update
del /F /Q update.zip
start %taskname%
del /F /Q update.bat
