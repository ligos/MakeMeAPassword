rem Clean before building
dotnet clean -c Release

rem Publish for .NET 8.0
dotnet publish Web -c Release -f net8.0

rem Delete config files so we can deploy
del /q Web\bin\Release\net8.0\publish\appsettings*.json
del /q Web\bin\Release\net8.0\publish\nlog.config
del /q Web\bin\Release\net8.0\publish\web.config

pause