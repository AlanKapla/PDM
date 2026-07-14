# 1. Runtime base (z curl do healthchecka)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app

# 2. Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 2.1. Kopiujemy same csproj-y (cache pod restore)
COPY src/Business/Business.csproj src/Business/
COPY src/Business.AIAgent/Business.AIAgent.csproj src/Business.AIAgent/
COPY src/Chat/Chat.csproj src/Chat/
COPY src/CQRS/CQRS.csproj src/CQRS/
COPY src/Entities/Entities.csproj src/Entities/
COPY src/FileUpload/FileUpload.csproj src/FileUpload/
COPY src/Models/Models.csproj src/Models/
COPY src/Repositories/Repositories.csproj src/Repositories/
COPY src/WebApi/WebApi.csproj src/WebApi/

RUN dotnet restore "src/WebApi/WebApi.csproj" -r linux-x64

# 2.2. Kopiujemy resztę solution
COPY . .

# 2.3. Build z katalogu WebApi
WORKDIR /src/src/WebApi
RUN dotnet build "WebApi.csproj" -c $BUILD_CONFIGURATION -r linux-x64 --no-self-contained -o /app/build

# 3. Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/src/WebApi
RUN dotnet publish "WebApi.csproj" -c $BUILD_CONFIGURATION -r linux-x64 --no-self-contained -o /app/publish /p:UseAppHost=false

# 4. Finalny obraz – TEN STAGE MUSI BYĆ OSTATNI
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# upewnij się, że DLL nazywa się dokładnie WebApi.dll
ENTRYPOINT ["dotnet", "WebApi.dll"]
