FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY GatumsGourmet.sln .
COPY src/GatumsGourmet.Domain/GatumsGourmet.Domain.csproj src/GatumsGourmet.Domain/
COPY src/GatumsGourmet.Application/GatumsGourmet.Application.csproj src/GatumsGourmet.Application/
COPY src/GatumsGourmet.Infrastructure/GatumsGourmet.Infrastructure.csproj src/GatumsGourmet.Infrastructure/
COPY src/GatumsGourmet.Api/GatumsGourmet.Api.csproj src/GatumsGourmet.Api/
RUN dotnet restore src/GatumsGourmet.Api/GatumsGourmet.Api.csproj
COPY . .
RUN dotnet publish src/GatumsGourmet.Api/GatumsGourmet.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GatumsGourmet.Api.dll"]
