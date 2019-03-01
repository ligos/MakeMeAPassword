rem Clean before building
dotnet clean -c Release

rem Publish for .NET Core 2.1
dotnet publish Web.NetCore -c Release -f netcoreapp2.1 

rem Build deployment 7z file for .NET Core 2.1, without config files
del /q MakeMeAPassword.netcoreapp21.7z
cd Web.NetCore\bin\Release\netcoreapp2.1\publish
"C:\Program Files\7-Zip\7z.exe" a -mx1 -x!appsettings*.json -x!nlog.config -x!web.config ..\..\..\..\..\MakeMeAPassword.netcoreapp21.7z * 

pause