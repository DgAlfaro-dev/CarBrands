# Build stage: SDK image to restore, build and publish.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Only the .csproj is copied first so restore uses layer caching and code
# changes do not invalidate this step.
COPY src/PruebaBackend.Api/PruebaBackend.Api.csproj src/PruebaBackend.Api/
RUN dotnet restore src/PruebaBackend.Api/PruebaBackend.Api.csproj

COPY src/PruebaBackend.Api/ src/PruebaBackend.Api/
RUN dotnet publish src/PruebaBackend.Api/PruebaBackend.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Runtime stage: ASP.NET Core runtime only (minimal image).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8 images listen on 8080 as a non-privileged user. The bind address is
# explicit IPv4: PaaS hosts such as Render do not support IPv6 and expect
# 0.0.0.0 (their docs call this out as a common misconfiguration).
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
# Workstation GC instead of server GC: free PaaS instances ship 512 MB and
# server GC heap reservation can exhaust them during startup.
ENV DOTNET_gcServer=0
EXPOSE 8080

ENTRYPOINT ["dotnet", "PruebaBackend.Api.dll"]
