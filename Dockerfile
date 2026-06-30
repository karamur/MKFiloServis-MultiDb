# syntax=docker/dockerfile:1.7
# ============================================================
# 🐳 MKFiloServis.Web — Multi-stage Dockerfile (.NET 10)
# ============================================================

# ─── 1) BUILD STAGE ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Sadece csproj'ları önce kopyala → restore katmanı cache'lensin
COPY ["MKFiloServis.Web/MKFiloServis.Web.csproj", "MKFiloServis.Web/"]
COPY ["MKFiloServis.Shared/MKFiloServis.Shared.csproj", "MKFiloServis.Shared/"]
RUN dotnet restore "MKFiloServis.Web/MKFiloServis.Web.csproj"

# Tüm kaynak kodu kopyala
COPY MKFiloServis.Web/   MKFiloServis.Web/
COPY MKFiloServis.Shared/ MKFiloServis.Shared/

WORKDIR /src/MKFiloServis.Web
RUN dotnet build "MKFiloServis.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore

# ─── 2) PUBLISH STAGE ─────────────────────────────────────────
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MKFiloServis.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─── 3) RUNTIME STAGE ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Yararlı runtime paketleri (TZ, sertifika, healthcheck için curl)
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       curl \
       tzdata \
       ca-certificates \
       libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# Türkiye saat dilimi
ENV TZ=Europe/Istanbul \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

# Kalıcı veri için klasörler
RUN mkdir -p /app/data /app/uploads /app/logs /app/Backups /app/keys \
    && chown -R 1000:1000 /app

# Non-root kullanıcı (güvenlik)
USER 1000:1000

COPY --from=publish --chown=1000:1000 /app/publish .

EXPOSE 8080

# Healthcheck (Blazor sayfası 200 dönüyorsa ayakta kabul)
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "MKFiloServis.Web.dll"]

