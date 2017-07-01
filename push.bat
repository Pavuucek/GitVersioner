cd chocolatey
choco push
cd ..\nuget
nuget pack gitversioner.nuget.nuspec
nuget.exe push .\ -Source https://www.nuget.org/api/v2/package
cd ..