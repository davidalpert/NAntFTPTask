set FTPDLL="edtftpnet-1.1.3.dll"
del /F *.dll
del /F *.exe
copy /Y ..\bin\*.dll .
copy /Y ..\bin\%FTPDLL% test.dll
csc /out:rundemo.exe /target:exe /reference:%FTPDLL% Demo.cs
rundemo.exe %1 %2 %3

