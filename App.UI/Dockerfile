FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce projeyi kopyalayýp restore edin
COPY ["App.UI/App.UI.csproj", "App.UI/"]
COPY ["App.Shared/App.Shared.csproj", "App.Shared/"]
COPY ["App.Api/App.Api.csproj", "App.Api/"]
COPY ["Services/App.Services.csproj", "Services/"]
COPY ["Repositories/App.Repositories.csproj", "Repositories/"]
RUN dotnet restore "App.UI/App.UI.csproj"

# Ardýndan tüm kodu kopyalayýn
COPY . .

# appsettings.json çakýþmalarýný çözün
RUN rm -f App.Api/appsettings*.json

# Build iþlemini TreatWarningsAsErrors=false ile yapýn
WORKDIR "/src/App.UI"
RUN dotnet publish "App.UI.csproj" -c Release -o /app/publish /p:TreatWarningsAsErrors=false /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "App.UI.dll"]