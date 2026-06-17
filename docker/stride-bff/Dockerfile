FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/src/BFF/STRIDE.BFF/STRIDE.BFF.csproj",                      "BFF/STRIDE.BFF/"]
COPY ["backend/src/BuildingBlocks/STRIDE.BuildingBlocks.Domain/STRIDE.BuildingBlocks.Domain.csproj",              "BuildingBlocks/STRIDE.BuildingBlocks.Domain/"]
COPY ["backend/src/BuildingBlocks/STRIDE.BuildingBlocks.Application/STRIDE.BuildingBlocks.Application.csproj",    "BuildingBlocks/STRIDE.BuildingBlocks.Application/"]
COPY ["backend/src/BuildingBlocks/STRIDE.BuildingBlocks.Infrastructure/STRIDE.BuildingBlocks.Infrastructure.csproj","BuildingBlocks/STRIDE.BuildingBlocks.Infrastructure/"]

RUN dotnet restore "BFF/STRIDE.BFF/STRIDE.BFF.csproj"

COPY backend/src .
RUN dotnet publish "BFF/STRIDE.BFF/STRIDE.BFF.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "STRIDE.BFF.dll"]
