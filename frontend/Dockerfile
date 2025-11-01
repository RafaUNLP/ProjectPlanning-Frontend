# Etapa 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Copiar package.json y package-lock.json (o yarn.lock si usas yarn)
COPY frontend/package.json .
COPY frontend/package-lock.json .
# Si usas yarn, descomenta la siguiente línea y comenta la anterior
# COPY frontend/yarn.lock .

# Instalar dependencias
RUN npm install
# Si usas yarn, descomenta la siguiente línea y comenta la anterior
# RUN yarn install

# Copiar el resto del código fuente
COPY frontend/ .

# Construir la aplicación
RUN npm run build

# Etapa 2: Serve (usando Nginx)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Eliminar el contenido por defecto de Nginx
RUN rm -rf ./*

# Copiar los archivos construidos desde la etapa de build
COPY --from=build /app/dist .

# Copiar la configuración de Nginx para Vue (SPA)
# Necesitaremos crear este archivo 'nginx.conf'
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Exponer el puerto 80
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]