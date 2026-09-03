# ==============================================================================
# 1. Build Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files for caching dotnet restore
COPY ["PickleHub.System/PickleHub.System.csproj", "PickleHub.System/"]
COPY ["PickleHub.Common/PickleHub.Common.csproj", "PickleHub.Common/"]

RUN dotnet restore "PickleHub.System/PickleHub.System.csproj"

# Copy source code and build
COPY ["PickleHub.System/", "PickleHub.System/"]
COPY ["PickleHub.Common/", "PickleHub.Common/"]

WORKDIR "/src/PickleHub.System"
RUN dotnet publish "PickleHub.System.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# 2. Runtime Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PickleHub.System.dll"]
