FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 5277
EXPOSE 5278

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/GetData.Api/GetData.Api.csproj", "tests/GetData.Api/"]
RUN dotnet restore "tests/GetData.Api/GetData.Api.csproj"
COPY . .
WORKDIR "/src/tests/GetData.Api"
RUN dotnet build "./GetData.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GetData.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GetData.Api.dll"]
