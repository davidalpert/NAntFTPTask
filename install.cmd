@set config=%1
@if (%config%)==() set config=release

@set nantTaskDir="c:\usr\local\nant-0.85-rc1\build\net-1.1.win32\nant-0.85-debug\bin\tasks\"

@set nantTaskDir="c:\usr\local\nant-0.85-rc1\bin\tasks\"

@echo installing to %nantTaskDir%
@echo.
@echo creating directory...
@mkdir %nantTaskDir%
@echo copying files...
@echo.
@copy bin\%config%\*.dll %nantTaskDir%
@if (%config%)==(debug) copy bin\%config%\*.pdb %nantTaskDir%

