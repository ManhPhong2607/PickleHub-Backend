# ==============================================================================
# 1. BUILD ALL MICROSERVICES STAGE
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files for layer caching
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
COPY PickleHub.MockExternalServices/PickleHub.MockExternalServices.csproj PickleHub.MockExternalServices/

# Restore all projects at once
RUN dotnet restore PickleHub.sln

# Copy all source files
COPY . .

# Publish all projects
RUN dotnet publish PickleHub.Gateway/PickleHub.Gateway.csproj -c Release -o /app/gateway /p:UseAppHost=false
RUN dotnet publish PickleHub.Authen/PickleHub.Authen.csproj -c Release -o /app/authen /p:UseAppHost=false
RUN dotnet publish PickleHub.Catalog/PickleHub.Catalog.csproj -c Release -o /app/catalog /p:UseAppHost=false
RUN dotnet publish PickleHub.Customers/PickleHub.Customers.csproj -c Release -o /app/customers /p:UseAppHost=false
RUN dotnet publish PickleHub.System/PickleHub.System.csproj -c Release -o /app/system /p:UseAppHost=false
RUN dotnet publish PickleHub.Inventory/PickleHub.Inventory.csproj -c Release -o /app/inventory /p:UseAppHost=false
RUN dotnet publish PickleHub.AuditLog/PickleHub.AuditLog.csproj -c Release -o /app/auditlog /p:UseAppHost=false
RUN dotnet publish PickleHub.CartOrder/PickleHub.CartOrder.csproj -c Release -o /app/cartorder /p:UseAppHost=false
RUN dotnet publish PickleHub.Payment/PickleHub.Payment.csproj -c Release -o /app/payment /p:UseAppHost=false
RUN dotnet publish PickleHub.Notification/PickleHub.Notification.csproj -c Release -o /app/notification /p:UseAppHost=false
RUN dotnet publish PickleHub.Review/PickleHub.Review.csproj -c Release -o /app/review /p:UseAppHost=false
RUN dotnet publish PickleHub.Blog/PickleHub.Blog.csproj -c Release -o /app/blog /p:UseAppHost=false

# ==============================================================================
# 2. RUNTIME STAGE WITH SUPERVISOR (ALL-IN-ONE CONTAINER)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

RUN apk add --no-cache supervisor bash

# Low memory profile for Render Free 512MB RAM
ENV DOTNET_gcServer=0
ENV DOTNET_TieredPGO=0

# Copy all published outputs
COPY --from=build /app /app

# Copy supervisor config
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

EXPOSE 8080

CMD ["/usr/bin/supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
