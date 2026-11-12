# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:11.0@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS build
WORKDIR /source

COPY CardDemo.Azure.slnx Directory.Build.props global.json ./
COPY src/CardDemo.Modern/CardDemo.Modern.csproj src/CardDemo.Modern/packages.lock.json src/CardDemo.Modern/
RUN dotnet restore src/CardDemo.Modern/CardDemo.Modern.csproj --locked-mode

COPY src/CardDemo.Modern/ src/CardDemo.Modern/
COPY app/data/ASCII/ app/data/ASCII/
RUN dotnet publish src/CardDemo.Modern/CardDemo.Modern.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra@sha256:f5b3b2e2e548828d50e349726f51a5de001286f02c4bbde77db0dd34eb9f55ff AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "CardDemo.Modern.dll"]
