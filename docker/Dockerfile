FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia apenas os .csproj primeiro (cache de restore em camadas)
COPY GatumsGourmet.sln .
COPY src/GatumsGourmet.Domain/GatumsGourmet.Domain.csproj src/GatumsGourmet.Domain/
COPY src/GatumsGourmet.Application/GatumsGourmet.Application.csproj src/GatumsGourmet.Application/
COPY src/GatumsGourmet.Infrastructure/GatumsGourmet.Infrastructure.csproj src/GatumsGourmet.Infrastructure/
COPY src/GatumsGourmet.Api/GatumsGourmet.Api.csproj src/GatumsGourmet.Api/
RUN dotnet restore src/GatumsGourmet.Api/GatumsGourmet.Api.csproj

# Copia o restante e publica
COPY . .
RUN dotnet publish src/GatumsGourmet.Api/GatumsGourmet.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GatumsGourmet.Api.dll"]
