# Dockerfile

# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project file first for Docker layer caching
COPY secure-workflow-system.csproj ./

# Restore only the application project
RUN dotnet restore secure-workflow-system.csproj --verbosity minimal

# Copy the remainder of the source
COPY . .

# Publish the application project only
RUN dotnet publish secure-workflow-system.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# Temporary Check after publish
RUN find /app/publish -path "*_framework*" -maxdepth 5 -print || true
RUN test -f /app/publish/wwwroot/_framework/blazor.web.js

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "secure-workflow-system.dll"]