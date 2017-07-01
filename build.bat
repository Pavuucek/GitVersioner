@echo off
rem %windir%\Microsoft.Net\Framework\v4.0.30319\msbuild 
call "%VS140COMNTOOLS%..\..\VC\vcvarsall.bat"
rem build once...
msbuild GitVersioner.sln /m /property:Configuration=Release /property:Platform="Any CPU"
if exist bin\release\gitversioner.exe bin\release\gitversioner.exe ba
if exist bin\release\gitversioner.exe bin\release\gitversioner.exe a
rem build second time to get exe with proper version
msbuild GitVersioner.sln /m /property:Configuration=Release /property:Platform="Any CPU"