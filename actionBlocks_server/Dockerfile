FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ActionBlocks.Server.sln ./
COPY src/ActionBlocks.Game/ActionBlocks.Game.csproj src/ActionBlocks.Game/
COPY src/ActionBlocks.Server/ActionBlocks.Server.csproj src/ActionBlocks.Server/
COPY tests/ActionBlocks.Game.Tests/ActionBlocks.Game.Tests.csproj tests/ActionBlocks.Game.Tests/
RUN dotnet restore

COPY . .
RUN dotnet test --no-restore -c Release \
 && dotnet publish src/ActionBlocks.Server/ActionBlocks.Server.csproj --no-restore -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ActionBlocks.Server.dll"]
