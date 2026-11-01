# Se construye desde la raíz del repo (contexto = .) porque necesita la carpeta Contracts.

# Etapa 1: compilar con el SDK.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/Inventory.Worker/Inventory.Worker.csproj src/Inventory.Worker/
RUN dotnet restore src/Inventory.Worker/Inventory.Worker.csproj
COPY src/Contracts src/Contracts
COPY src/Inventory.Worker src/Inventory.Worker
RUN dotnet publish src/Inventory.Worker/Inventory.Worker.csproj -c Release -o /app --no-restore

# Etapa 2: imagen final liviana. Un worker no sirve HTTP, por eso usa "runtime" y no "aspnet".
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine
WORKDIR /app
COPY --from=build /app .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
USER app
ENTRYPOINT ["dotnet", "Inventory.Worker.dll"]
