# syntax=docker/dockerfile:1

# ---- Build ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the dependency layer is cached between code changes.
COPY src/DeployRequests.Api/DeployRequests.Api.csproj src/DeployRequests.Api/
RUN dotnet restore src/DeployRequests.Api/DeployRequests.Api.csproj

COPY src/ src/
RUN dotnet publish src/DeployRequests.Api/DeployRequests.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime ---------------------------------------------------------------
# Chiseled image: no shell, no package manager, runs as non-root by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "DeployRequests.Api.dll"]
