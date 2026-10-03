# Build context is the repo root (multi-project solution), e.g.:
#   docker build -f src/Api/Dockerfile -t receiptiq-api .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Project files only, so `dotnet restore` is cached across builds that only change source.
COPY ReceiptIQ.sln .
COPY src/Api/Api.csproj src/Api/
COPY src/Application/Application.csproj src/Application/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/Api/Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Health checks for docker compose / orchestrators that don't bring their own HTTP probe.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

# Created (and owned) before the volume is mounted over it, so a fresh named volume
# inherits this ownership instead of defaulting to root — the app can't write into
# a root-owned mount point once it drops to a non-root user below.
RUN mkdir -p /app/storage/receipts && chown -R $APP_UID:$APP_UID /app/storage

# $APP_UID is baked into the base image — a non-root account the image already owns.
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Api.dll"]
