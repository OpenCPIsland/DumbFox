@ECHO OFF
SET NAME=Disney Mix and CPI WebAPI Server launcher
TITLE %NAME%

@echo off
:checker
tasklist /FI "IMAGENAME eq httpd.exe" 2>NUL | find /I /N "httpd">NUL
if "%ERRORLEVEL%"=="1" goto start_web

:start_web
ECHO.
ECHO Disney Mix and CPI Web Server
ECHO.
ECHO Included:
ECHO - Apache 2.4.67
start http://127.0.0.1

"%CD%\bin\httpd.exe"
