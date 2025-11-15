FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5144

ENV ASPNETCORE_URLS=http://+:5144

USER app

# Build the client
FROM node:slim AS client-build
WORKDIR /src
COPY ./src/Client/package*.json ./src/Client/
RUN cd ./src/Client && npm ci
COPY ./src/Client ./src/Client

# Build to ../Caramel.API/wwwroot as configured in vite.config.ts
RUN cd ./src/Client && npm run build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["src/Caramel.API/Caramel.API.csproj", "src/Caramel.API/"]
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
