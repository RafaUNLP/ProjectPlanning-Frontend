# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copiar los archivos de solución y proyecto MANTENIENDO LA ESTRUCTURA
# Copia 'backend/backend.sln' a '/src/backend/backend.sln'
COPY backend/backend.sln ./backend/
# Copia 'backend/backend.csproj' a '/src/backend/backend.csproj'
COPY backend/backend.csproj ./backend/

# 2. Restaurar dependencias
# Ahora sí encontrará el .sln y su .csproj relativo
RUN dotnet restore backend/backend.sln

# 3. Copiar el resto del código fuente del backend
COPY backend/ ./backend/

# 4. Publicar la aplicación
WORKDIR /src/backend
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto que usa el backend
EXPOSE 5165

ENTRYPOINT ["dotnet", "backend.dll"]