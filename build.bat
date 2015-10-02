@echo off
%windir%\Microsoft.Net\Framework\v4.0.30319\msbuild GitVersioner.sln  /property:Configuration=Release /property:Platform="Any CPU"
