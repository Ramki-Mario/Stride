FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/src/Host/STRIDE.Host/STRIDE.Host.csproj",                   "Host/STRIDE.Host/"]
COPY ["backend/src/BFF/STRIDE.BFF/STRIDE.BFF.csproj",                      "BFF/STRIDE.BFF/"]
COPY ["backend/src/BuildingBlocks/STRIDE.BuildingBlocks.Domain/STRIDE.BuildingBlocks.Domain.csproj",              "BuildingBlocks/STRIDE.BuildingBlocks.Domain/"]
COPY ["backend/src/BuildingBlocks/STRIDE.BuildingBlocks.Application/STRIDE.BuildingBlocks.Application.csproj",    "BuildingBlocks/STRIDE.BuildingBlocks.Application/"]
COPY ["backend/src/BuildingBlocks/STRIDE.BuildingBlocks.Infrastructure/STRIDE.BuildingBlocks.Infrastructure.csproj","BuildingBlocks/STRIDE.BuildingBlocks.Infrastructure/"]
COPY ["backend/src/Modules/Identity/STRIDE.Modules.Identity.Domain/STRIDE.Modules.Identity.Domain.csproj",             "Modules/Identity/STRIDE.Modules.Identity.Domain/"]
COPY ["backend/src/Modules/Identity/STRIDE.Modules.Identity.Application/STRIDE.Modules.Identity.Application.csproj",   "Modules/Identity/STRIDE.Modules.Identity.Application/"]
COPY ["backend/src/Modules/Identity/STRIDE.Modules.Identity.Infrastructure/STRIDE.Modules.Identity.Infrastructure.csproj","Modules/Identity/STRIDE.Modules.Identity.Infrastructure/"]
COPY ["backend/src/Modules/Identity/STRIDE.Modules.Identity.API/STRIDE.Modules.Identity.API.csproj",                   "Modules/Identity/STRIDE.Modules.Identity.API/"]
COPY ["backend/src/Modules/Workflows/STRIDE.Modules.Workflows.Domain/STRIDE.Modules.Workflows.Domain.csproj",              "Modules/Workflows/STRIDE.Modules.Workflows.Domain/"]
COPY ["backend/src/Modules/Workflows/STRIDE.Modules.Workflows.Application/STRIDE.Modules.Workflows.Application.csproj",    "Modules/Workflows/STRIDE.Modules.Workflows.Application/"]
COPY ["backend/src/Modules/Workflows/STRIDE.Modules.Workflows.Infrastructure/STRIDE.Modules.Workflows.Infrastructure.csproj","Modules/Workflows/STRIDE.Modules.Workflows.Infrastructure/"]
COPY ["backend/src/Modules/Workflows/STRIDE.Modules.Workflows.API/STRIDE.Modules.Workflows.API.csproj",                    "Modules/Workflows/STRIDE.Modules.Workflows.API/"]

RUN dotnet restore "Host/STRIDE.Host/STRIDE.Host.csproj"

COPY backend/src .
RUN dotnet publish "Host/STRIDE.Host/STRIDE.Host.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "STRIDE.Host.dll"]
