FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SS14.MaintainerBot/SS14.MaintainerBot.csproj", "SS14.MaintainerBot/"]
RUN dotnet restore "SS14.MaintainerBot/SS14.MaintainerBot.csproj"
COPY . .
WORKDIR "/src/SS14.MaintainerBot"
RUN dotnet build "SS14.MaintainerBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SS14.MaintainerBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SS14.MaintainerBot.dll"]
