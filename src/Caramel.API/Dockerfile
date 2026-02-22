FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5144

ENV ASPNETCORE_URLS=http://+:5144

USER app

# Build the client
FROM node:alpine AS client-build
WORKDIR /src
COPY ./src/Client/package*.json ./src/Client/
RUN cd ./src/Client && npm ci
COPY ./src/Client ./src/Client

# Build to ../Caramel.API/wwwroot as configured in vite.config.ts
RUN cd ./src/Client && npm run build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Caramel.sln", "./"]
COPY ["src/Caramel.AI/Caramel.AI.csproj", "src/Caramel.AI/"]
COPY ["src/Caramel.API/Caramel.API.csproj", "src/Caramel.API/"]
COPY ["src/Caramel.Application/Caramel.Application.csproj", "src/Caramel.Application/"]
COPY ["src/Caramel.Cache/Caramel.Cache.csproj", "src/Caramel.Cache/"]
COPY ["src/Caramel.Core/Caramel.Core.csproj", "src/Caramel.Core/"]
COPY ["src/Caramel.Database/Caramel.Database.csproj", "src/Caramel.Database/"]
COPY ["src/Caramel.Discord/Caramel.Discord.csproj", "src/Caramel.Discord/"]
COPY ["src/Caramel.Domain/Caramel.Domain.csproj", "src/Caramel.Domain/"]
COPY ["src/Caramel.GRPC/Caramel.GRPC.csproj", "src/Caramel.GRPC/"]
COPY ["src/Caramel.Notifications/Caramel.Notifications.csproj", "src/Caramel.Notifications/"]
COPY ["src/Caramel.Service/Caramel.Service.csproj", "src/Caramel.Service/"]
COPY ["src/Caramel.Twitch/Caramel.Twitch.csproj", "src/Caramel.Twitch/"]
RUN dotnet restore "src/Caramel.API/Caramel.API.csproj"
COPY . .
COPY --from=client-build /src/src/Caramel.API/wwwroot ./src/Caramel.API/wwwroot
WORKDIR "/src/src/Caramel.API"
RUN dotnet build "Caramel.API.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Caramel.API.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Caramel.API.dll"]
