@echo off
cd chocolatey
..\bin\release\gitversioner w -f=gitversioner.choco.nuspec
choco pack gitversioner.choco.nuspec
..\bin\release\gitversioner r -f=gitversioner.choco.nuspec
cd ..\nuget
..\bin\release\gitversioner w -f=gitversioner.nuget.nuspec
nuget pack gitversioner.nuget.nuspec
..\bin\release\gitversioner r -f=gitversioner.nuget.nuspec
cd ..