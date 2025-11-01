Notas del backend:

- Para levantar la BD por primera vez, se debe utilizar Dcker y Entity Framework:
    0 - En la carpeta general del proyecto, levantar el docker con:
        docker compose up -d
    1 - Ubicarse en la carpeta backend
    2 - Crear la primer migración con: 
            dotnet tool run dotnet-ef migrations add "Migración inicial" (si falla, se puede probar corriendo el backend en debug con F5 y volviendo a crear la migración)
    3 - Opcionalmente, ver todas las migraciones existentes con:
            dotnet tool run dotnet-ef migrations list
    4 - Correr la migración con dotnet tool run dotnet-ef database update
    
    NOTA: en caso de querer borrar una migración creada, se puede borrar la última con:
        dotnet tool run dotnet-ef migrations remove

- Para correr la aplicación de .NET en modo debug, basta con tocar F5

- Por las dudas dejo dónde me levantó el Swagger a mí, en caso de que no se abra sólo:
    https://localhost:7166/swagger/index.html



##Docker explicacion
Contexto de Build: El docker-compose.yml raíz usa context: . y especifica la ruta a cada Dockerfile (backend/Dockerfile y frontend/Dockerfile).

Red de Docker:

El frontend (en el navegador, en http://localhost:8080) se comunicará con el backend en http://localhost:5165 (gracias al ports: - "5165:5165"). Tu archivo frontend/src/api.ts ya apunta a esta dirección, así que funcionará sin cambios.

El backend se comunicará con la base de datos usando el nombre del servicio db (gracias a la variable de entorno ConnectionStrings__DefaultConnection=Host=db...). Docker se encargará de resolver db a la IP interna del contenedor de Postgres.

Puertos:

Frontend: Accesible en http://localhost:8080 (mapeado al puerto 80 del contenedor Nginx).

Backend: Accesible en http://localhost:5165 (mapeado al puerto 5165 del contenedor .NET).

Base de Datos: Accesible en localhost:5434 (mapeado al puerto 5432 del contenedor Postgres).

Hay un docker compose para produccion y otro para dev, para levantarlo:
``docker compose -f docker-compose.dev.yml up -d --build``