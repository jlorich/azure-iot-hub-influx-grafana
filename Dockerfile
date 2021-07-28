# Build
FROM mcr.microsoft.com/dotnet/sdk:5.0 as build

WORKDIR /build

COPY /azure-iot-hub-influx-connector.csproj .
RUN dotnet restore azure-iot-hub-influx-connector.csproj

COPY src ./src
RUN dotnet build azure-iot-hub-influx-connector.csproj

# Run
FROM mcr.microsoft.com/dotnet/runtime:5.0 as run
EXPOSE 80/tcp
WORKDIR /app
COPY --from=build /build/bin/Debug/net5.0/ /app/

ENTRYPOINT ["dotnet", "azure-iot-hub-influx-connector.dll"]