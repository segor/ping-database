FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["ping-db/ping-db.csproj", "ping-db/"]
RUN dotnet restore "ping-db/ping-db.csproj"
COPY . .
WORKDIR "/src/ping-db"
RUN dotnet build "ping-db.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ping-db.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ping-db.dll"]