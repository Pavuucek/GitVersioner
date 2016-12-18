@echo off
rem %windir%\Microsoft.Net\Framework\v4.0.30319\msbuild 
call "%VS140COMNTOOLS%..\..\VC\vcvarsall.bat"
msbuild GitVersioner.sln /m /property:Configuration=Release /property:Platform="Any CPU"
if exist bin\release\gitversioner.exe bin\release\gitversioner.exe ba