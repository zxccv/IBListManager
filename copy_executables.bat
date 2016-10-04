@echo off
if "%~1"=="" (Exit /B)
echo Creating %1
mkdir Executables
rd /S /Q Executables\%1
mkdir Executables\%1
mkdir Executables\%1\Manager
mkdir Executables\%1\Service
copy /Y InfoBaseListDataClasses\bin\%1\InfoBaseListDataClasses.dll /B Executables\%1\Service\ /B
copy /Y InfoBaseListService\bin\%1\InfoBaseListService.exe /B Executables\%1\Service\ /B
copy /Y InfoBaseListServiceInstaller\bin\%1\InfoBaseListServiceInstaller.exe /B Executables\%1\Service\ /B
copy /Y InfoBaseListServiceInstaller\bin\%1\Install.bat /B Executables\%1\Service\ /B
copy /Y InfoBaseListServiceInstaller\bin\%1\Uninstall.bat /B Executables\%1\Service\ /B


copy /Y InfoBaseListDataClasses\bin\%1\InfoBaseListDataClasses.dll /B Executables\%1\Manager\ /B
copy /Y UDPServer\bin\%1\InfoBaseListUDPServer.dll /B Executables\%1\Manager\ /B
copy /Y InfoBaseListManager\bin\%1\InfoBaseListManager.exe /B Executables\%1\Manager\ /B


