# ==============================================================================
# 1. BUILD STAGE: SINGLE PROCESS MODULAR MONOLITH (PickleHub.App)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and all project files for layer caching
COPY PickleHub.sln ./
COPY PickleHub.Common/PickleHub.Common.csproj PickleHub.Common/
COPY PickleHub.Gateway/PickleHub.Gateway.csproj PickleHub.Gateway/
COPY PickleHub.Authen/PickleHub.Authen.csproj PickleHub.Authen/
COPY PickleHub.Catalog/PickleHub.Catalog.csproj PickleHub.Catalog/
COPY PickleHub.Customers/PickleHub.Customers.csproj PickleHub.Customers/
COPY PickleHub.System/PickleHub.System.csproj PickleHub.System/
COPY PickleHub.Inventory/PickleHub.Inventory.csproj PickleHub.Inventory/
COPY PickleHub.AuditLog/PickleHub.AuditLog.csproj PickleHub.AuditLog/
COPY PickleHub.CartOrder/PickleHub.CartOrder.csproj PickleHub.CartOrder/
COPY PickleHub.Payment/PickleHub.Payment.csproj PickleHub.Payment/
COPY PickleHub.Notification/PickleHub.Notification.csproj PickleHub.Notification/
COPY PickleHub.Review/PickleHub.Review.csproj PickleHub.Review/
COPY PickleHub.Blog/PickleHub.Blog.csproj PickleHub.Blog/
COPY PickleHub.App/PickleHub.App.csproj PickleHub.App/

# Restore all projects at once
RUN dotnet restore PickleHub.sln

# Copy full source tree
COPY . .

# Publish ONLY PickleHub.App (contains all 12 modules)
RUN dotnet publish PickleHub.App/PickleHub.App.csproj -c Release -o /app /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

# ==============================================================================
# 2. RUNTIME STAGE: ULTRA LEAN SINGLE PROCESS HOST (< 160MB RAM)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# GC & Threading optimizations for Render Free 512MB RAM
ENV DOTNET_gcServer=0
ENV DOTNET_TieredPGO=0
ENV DOTNET_GCTrimCommit=1
ENV DOTNET_GCHeapHardLimit=80000000
ENV DOTNET_EnableWriteXorExecute=0
ENV DOTNET_ThreadPool_UnfairSemaphore=1

# Copy published application
COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "PickleHub.App.dll"]
