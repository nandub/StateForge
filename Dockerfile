FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/StateForge.KestrelHarness/StateForge.KestrelHarness.csproj
RUN dotnet publish src/StateForge.KestrelHarness/StateForge.KestrelHarness.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    STATEFORGE_ROOT_PATH=/data/stateforge \
    STATEFORGE_SNAPSHOT_PATH=/data/stateforge/stateforge-store-snapshot.json \
    STATEFORGE_COMPRESSION=true \
    STATEFORGE_ENCRYPTION=false \
    STATEFORGE_PROTECTION_MODE=none \
    STATEFORGE_KEEP_BACKUPS=false \
    STATEFORGE_SHARD_DEPTH=1 \
    STATEFORGE_ENABLE_DEMO_ENDPOINTS=false
RUN mkdir -p /data/stateforge && chown -R app:app /data/stateforge /app
COPY --from=build /app/publish .
RUN chown -R app:app /app
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "StateForge.KestrelHarness.dll"]
