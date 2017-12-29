@echo off
dotnet pack -c Release
nuget.exe push .\bin\Release\*.nupkg -Source https://www.nuget.org/api/v2/package
