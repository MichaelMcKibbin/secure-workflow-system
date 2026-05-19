FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage layer cache for restore
COPY secure-workflow-system.slnx ./
COPY secure-workflow-system.csproj ./
COPY secure-workflow-system.Tests.Components/secure-workflow-system.Tests.Components.csproj ./secure-workflow-system.Tests.Components/
COPY secure-workflow-system.Tests.Unit/secure-workflow-system.Tests.Unit.csproj ./secure-workflow-system.Tests.Unit/

# Restore the solution (use minimal/diagnostic temporarily if you need logs)
RUN dotnet restore secure-workflow-system.slnx --verbosity minimal

# Copy everything else and publish the app
COPY . .
RUN dotnet publish secure-workflow-system.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "secure-workflow-system.dll"]