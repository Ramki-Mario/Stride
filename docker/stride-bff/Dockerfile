FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy entire backend source before restore. The BFF depends on BuildingBlocks
# and other shared assemblies — copying all src avoids missing-reference errors.
COPY backend/src .
RUN dotnet restore "BFF/STRIDE.BFF/STRIDE.BFF.csproj"
RUN dotnet publish "BFF/STRIDE.BFF/STRIDE.BFF.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "STRIDE.BFF.dll"]
