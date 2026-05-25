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

# temporary check
RUN dotnet --info
RUN dotnet workload list

# Publish the application project only
RUN dotnet publish secure-workflow-system.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# Temporary Check after publish
RUN echo "Published _framework files:" && find /app/publish -path "*_framework*" -print || true
RUN echo "Published files containing blazor:" && find /app/publish -iname "*blazor*" -print || true
RUN echo "Static web asset manifest blazor entries:" && grep -i "blazor.web" /app/publish/*.staticwebassets*.json || true

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