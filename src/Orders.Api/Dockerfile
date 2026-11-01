# Se construye desde la raíz del repo (contexto = .) porque necesita la carpeta Contracts.

# Etapa 1: compilar con el SDK (imagen grande, solo se usa para construir).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Primero solo los .csproj: así Docker cachea el restore si el código no cambia sus dependencias.
COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/Orders.Api/Orders.Api.csproj src/Orders.Api/
RUN dotnet restore src/Orders.Api/Orders.Api.csproj
COPY src/Contracts src/Contracts
COPY src/Orders.Api src/Orders.Api
RUN dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app --no-restore

# Etapa 2: imagen final liviana (alpine) solo con el runtime y el resultado publicado.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
