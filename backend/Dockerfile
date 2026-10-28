# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primero solo los .csproj: la restauración de paquetes queda en caché si el código cambia.
COPY src/Turnos.Domain/Turnos.Domain.csproj src/Turnos.Domain/
COPY src/Turnos.Application/Turnos.Application.csproj src/Turnos.Application/
COPY src/Turnos.Infrastructure/Turnos.Infrastructure.csproj src/Turnos.Infrastructure/
COPY src/Turnos.Api/Turnos.Api.csproj src/Turnos.Api/
RUN dotnet restore src/Turnos.Api/Turnos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Turnos.Api/Turnos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/turnos.db"

# La base SQLite vive en un volumen, no dentro de la imagen.
RUN mkdir /data && chown $APP_UID /data
VOLUME /data

COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Turnos.Api.dll"]
