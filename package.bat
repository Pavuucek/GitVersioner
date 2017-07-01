@echo off
cd chocolatey
..\bin\release\gitversioner w gitversioner.choco.nuspec
choco pack gitversioner.choco.nuspec
..\bin\release\gitversioner r gitversioner.choco.nuspec
cd ..\nuget
..\bin\release\gitversioner w gitversioner.nuget.nuspec
nuget pack gitversioner.nuget.nuspec
..\bin\release\gitversioner r gitversioner.nuget.nuspec
cd ..