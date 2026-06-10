SET PACKAGEREPOSITORY=C:\repos\localpackages

dotnet build ..\Unir.Framework.Observability.Abstractions\Unir.Framework.Observability.Abstractions.csproj -c Release
dotnet pack ..\Unir.Framework.Observability.Abstractions\Unir.Framework.Observability.Abstractions.csproj -c Release
copy ..\Unir.Framework.Observability.Abstractions\bin\Release\*.nupkg %PACKAGEREPOSITORY%\
del ..\Unir.Framework.Observability.Abstractions\bin\Release\*.nupkg

dotnet build ..\Unir.Framework.Observability\Unir.Framework.Observability.csproj -c Release
dotnet pack ..\Unir.Framework.Observability\Unir.Framework.Observability.csproj -c Release
copy ..\Unir.Framework.Observability\bin\Release\*.nupkg %PACKAGEREPOSITORY%\
del ..\Unir.Framework.Observability\bin\Release\*.nupkg