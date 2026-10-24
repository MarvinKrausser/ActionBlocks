FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY --from=build /app/dist /usr/share/nginx/html
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh

# Full URL of the game hub as seen by the browser, e.g. https://blocks.example.com/hub.
# Empty = same origin + /hub (reverse proxy routes /hub to the server).
ENV SERVER_URL=""
EXPOSE 80
