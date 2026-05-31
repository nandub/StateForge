FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore StateForge.sln
RUN dotnet publish src/StateForge.KestrelHarness/StateForge.KestrelHarness.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV STATEFORGE_ROOT_PATH=/data/stateforge
ENV STATEFORGE_COMPRESSION=true
ENV STATEFORGE_ENCRYPTION=false
ENV STATEFORGE_KEEP_BACKUPS=false
ENV STATEFORGE_SHARD_DEPTH=1
RUN mkdir -p /data/stateforge
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "StateForge.KestrelHarness.dll"]
