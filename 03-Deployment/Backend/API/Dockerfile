FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Kopiujemy same csproj (cache restore)
COPY src/Business/Business.csproj src/Business/
COPY src/CQRS/CQRS.csproj src/CQRS/
COPY src/Entities/Entities.csproj src/Entities/
COPY src/Repositiories/Repositiories.csproj src/Repositiories/
COPY src/WebApi/WebApi.csproj src/WebApi/

RUN dotnet restore "src/WebApi/WebApi.csproj"

# 2. Kopiujemy resztę kodu
COPY . .

# 3. Build z katalogu WebApi
WORKDIR /src/src/WebApi
RUN dotnet build "WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/src/WebApi
RUN dotnet publish "WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
