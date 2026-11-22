# syntax=docker/dockerfile:1

# Single image: the ASP.NET Core API also serves the Angular screen
# (same origin, no CORS, one artifact to deploy).

# ---- Front-end build -------------------------------------------------------
FROM node:24-alpine AS web-build
WORKDIR /web

COPY web/package.json web/package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY web/ ./
RUN npx ng build --configuration production

# ---- API build -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
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

COPY --from=api-build /app/publish .
COPY --from=web-build /web/dist/web/browser ./wwwroot

USER $APP_UID
ENTRYPOINT ["dotnet", "DeployRequests.Api.dll"]
