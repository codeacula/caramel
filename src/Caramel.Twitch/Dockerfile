FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5146

ENV ASPNETCORE_URLS=http://+:5146

USER app

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
RUN dotnet restore "src/Caramel.Twitch/Caramel.Twitch.csproj"
COPY . .
WORKDIR "/src/src/Caramel.Twitch"
RUN dotnet build "Caramel.Twitch.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Caramel.Twitch.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Caramel.Twitch.dll"]
