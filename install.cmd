@set config=%1
@if (%config%)==() set config=release
@copy bin\%config%\*.dll c:\usr\local\nant-0.85-rc1\build\net-1.1.win32\nant-0.85-debug\bin\tasks
@if (%config%)==(debug) copy bin\%config%\*.pdb c:\usr\local\nant-0.85-rc1\build\net-1.1.win32\nant-0.85-debug\bin\tasks

