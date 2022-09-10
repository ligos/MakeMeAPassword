rem Clean before building
dotnet clean -c Release

rem Publish for .NET 6.0
dotnet publish Web.Net60 -c Release -f net6.0

rem Delete config files so we can deploy
del /q Web.Net60\bin\Release\net6.0\publish\appsettings*.json
del /q Web.Net60\bin\Release\net6.0\publish\nlog.config
del /q Web.Net60\bin\Release\net6.0\publish\web.config

pause