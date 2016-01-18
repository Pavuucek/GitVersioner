@echo off
if exist "cov-int" rd /q /s "cov-int"
if exist "coverity-GitVersioner.zip" del "coverity-GitVersioner.zip"
cov-build.exe --dir cov-int %windir%\Microsoft.Net\Framework\v4.0.30319\msbuild GitVersioner.sln /m /t:Rebuild /property:Configuration=Release /property:Platform="Any CPU"
7z a -tzip "coverity-GitVersioner.zip" "cov-int"
bin\release\gitversioner.exe w coverity-submit.bat
call coverity-submit.bat
bin\release\gitversioner.exe r coverity-submit.bat
if exist "cov-int" rd /q /s "cov-int"
pause