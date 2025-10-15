# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore
COPY ["App.UI/App.UI.csproj", "App.UI/"]
COPY ["App.Shared/App.Shared.csproj", "App.Shared/"]
COPY ["App.Api/App.Api.csproj", "App.Api/"]
COPY ["Services/App.Services.csproj", "Services/"]
COPY ["Repositories/App.Repositories.csproj", "Repositories/"]
RUN dotnet restore "App.UI/App.UI.csproj"

# Copy source
COPY . .

# API'nin appsettings'lerini SİL (çakışma olmasın)
RUN rm -f App.Api/appsettings*.json

WORKDIR "/src/App.UI"
RUN dotnet build "App.UI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "App.UI.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App.UI.dll"]