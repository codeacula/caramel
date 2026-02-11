FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5270

ENV ASPNETCORE_URLS=http://+:5270

USER app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["src/Caramel.Service/Caramel.Service.csproj", "src/Caramel.Service/"]
RUN dotnet restore "src/Caramel.Service/Caramel.Service.csproj"
COPY . .
WORKDIR "/src/src/Caramel.Service"
RUN dotnet build "Caramel.Service.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Caramel.Service.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Caramel.Service.dll"]
