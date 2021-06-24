rem Clean before building
dotnet clean -c Release

rem Publish for .NET Core 3.1
dotnet publish Web.NetCore -c Release -f netcoreapp3.1 

rem Delete config files so we can deploy
del /q Web.NetCore\bin\Release\netcoreapp3.1\publish\appsettings*.json
del /q Web.NetCore\bin\Release\netcoreapp3.1\publish\nlog.config
del /q Web.NetCore\bin\Release\netcoreapp3.1\publish\web.config
del /q Web.NetCore\bin\Release\netcoreapp3.1\publish\bypasskeys.txt

pause