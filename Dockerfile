# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first so dependency layers cache across code-only changes.
COPY Directory.Build.props ./
COPY src/InsuranceIntegration.Api/InsuranceIntegration.Api.csproj src/InsuranceIntegration.Api/
RUN dotnet restore src/InsuranceIntegration.Api/InsuranceIntegration.Api.csproj

COPY src/ src/
RUN dotnet publish src/InsuranceIntegration.Api/InsuranceIntegration.Api.csproj \
    -c Release -o /app --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# SQLite database lives on a mounted volume so data survives container replacement.
RUN mkdir -p /data && chown $APP_UID /data
VOLUME /data

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Integration="Data Source=/data/integration.db"

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080

# Probe the real /health endpoint using bash's /dev/tcp (no curl/wget in the aspnet image).
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["bash", "-c", "exec 3<>/dev/tcp/localhost/8080 && printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && grep -q '200' <&3"]

ENTRYPOINT ["dotnet", "InsuranceIntegration.Api.dll"]
