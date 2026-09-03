# ==============================================================================
# 1. Build Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files for caching dotnet restore
COPY ["PickleHub.CartOrder/PickleHub.CartOrder.csproj", "PickleHub.CartOrder/"]
COPY ["PickleHub.Common/PickleHub.Common.csproj", "PickleHub.Common/"]

RUN dotnet restore "PickleHub.CartOrder/PickleHub.CartOrder.csproj"

# Copy source code and build
COPY ["PickleHub.CartOrder/", "PickleHub.CartOrder/"]
COPY ["PickleHub.Common/", "PickleHub.Common/"]

WORKDIR "/src/PickleHub.CartOrder"
RUN dotnet publish "PickleHub.CartOrder.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "PickleHub.CartOrder.dll"]
