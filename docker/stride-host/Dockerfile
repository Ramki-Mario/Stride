FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy entire backend source so dotnet restore can resolve all module references.
# The Host project references 11 modules; listing them individually was fragile.
COPY backend/src .
RUN dotnet restore "Host/STRIDE.Host/STRIDE.Host.csproj"
RUN dotnet publish "Host/STRIDE.Host/STRIDE.Host.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "STRIDE.Host.dll"]
