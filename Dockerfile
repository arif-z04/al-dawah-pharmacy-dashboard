# =============================================================================
# Multi-Stage Dockerfile for Al-Dawah Pharma
# Builds both ASP.NET Core 10 Web API and bundles Vanilla JS Frontend into wwwroot
# =============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project definitions for optimized layer caching
COPY ["AlDawahPharma.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/Backend/Domain/Domain.csproj", "src/Backend/Domain/"]
COPY ["src/Backend/Application/Application.csproj", "src/Backend/Application/"]
COPY ["src/Backend/Infrastructure/Infrastructure.csproj", "src/Backend/Infrastructure/"]
COPY ["src/Backend/Api/Api.csproj", "src/Backend/Api/"]
COPY ["tests/AlDawahPharma.Tests/AlDawahPharma.Tests.csproj", "tests/AlDawahPharma.Tests/"]

# Restore dependencies
RUN dotnet restore "src/Backend/Api/Api.csproj"

# Copy entire source tree including Frontend assets
COPY . .

# Build and Publish in Release mode
WORKDIR "/src/src/Backend/Api"
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Configure default listening port and environment
ENV ASPNETCORE_URLS=http://+:5091
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5091

# Copy published application from build stage
COPY --from=build /app/publish .

# Start the application
ENTRYPOINT ["dotnet", "Api.dll"]
