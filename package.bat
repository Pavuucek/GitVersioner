@echo off
cd chocolatey
..\bin\release\gitversioner w gitversioner.choco.nuspec
choco pack gitversioner.choco.nuspec
choco push
..\bin\release\gitversioner r gitversioner.choco.nuspec
cd ..\nuget
..\bin\release\gitversioner w gitversioner.nuget.nuspec
nuget pack gitversioner.nuget.nuspec
nuget.exe push .\ -Source https://www.nuget.org/api/v2/package
..\bin\release\gitversioner r gitversioner.nuget.nuspec
cd ..