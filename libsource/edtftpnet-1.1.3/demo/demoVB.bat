set FTPDLL="edtftpnet-1.1.3.dll"
del /F *.dll
del /F *.exe
copy /Y ..\bin\*.dll .
copy /Y ..\bin\%FTPDLL% test.dll
vbc /out:rundemo.exe /target:exe /reference:%FTPDLL% Demo.vb
rundemo.exe %1 %2 %3

