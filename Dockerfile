# syntax=docker/dockerfile:1

# ---------- Stage 1: build ----------
# The SDK image (about 900 MB) is only used to compile; it never ships.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project file only: this layer is cached and re-runs
# only when the project's package references change, not on every code change.
COPY SmartMarketingPlatform/SmartMarketingPlatform.csproj SmartMarketingPlatform/
RUN dotnet restore SmartMarketingPlatform/SmartMarketingPlatform.csproj

# Then the source code, and publish a Release build.
COPY SmartMarketingPlatform/ SmartMarketingPlatform/
RUN dotnet publish SmartMarketingPlatform/SmartMarketingPlatform.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
# The ASP.NET runtime image (much smaller): no SDK, no source code, only the published app.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Folder for uploaded files, encrypted secrets and Data Protection keys (mounted as a volume).
# Created here and owned by the non-root "app" user, so the named volume inherits these permissions.
RUN mkdir -p /app/App_Data && chown -R $APP_UID /app/App_Data

COPY --from=build /app/publish .

# Never run as root inside the container.
USER $APP_UID

# The .NET images listen on port 8080 by default (ASPNETCORE_HTTP_PORTS=8080).
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartMarketingPlatform.dll"]
