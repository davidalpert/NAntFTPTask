rem
rem You need nunit in your PATH to run the tests
rem
@echo off
set FTPDLL="edtftpnet-test-1.1.3.dll"
del /F *.dll
copy /Y ..\bin\*.dll .
copy /Y ..\bin\%FTPDLL% test.dll
copy /Y test.config.passive %FTPDLL%.config
copy /Y %FTPDLL%.config test1.txt
nunit-console %FTPDLL%
rem
rem 120 second pause to allow earlier TIME_WAITs to expire
rem
ping -n 120 localhost>NUL
copy /Y test.config.active %FTPDLL%.config
copy /Y %FTPDLL%.config test1.txt
nunit-console %FTPDLL%
del /F *.dll
del /F test1.txt
del /F %FTPDLL%.config
@echo on
