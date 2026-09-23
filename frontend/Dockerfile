# Stage 1: Build context is the repo root: the Vite build imports the real backend
FROM node:20-alpine AS build

WORKDIR /app

COPY project/package.json project/package-lock.json ./

RUN npm ci

COPY project/ ./
COPY project/.env.docker ./.env

RUN npm run build

# Stage 2: Runtime: nginx serving the bundle and proxying /api -> backend 
FROM alpine:latest

# Disabled, loaded using docker-compose.yaml
#COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf

# Install NGINX and the module from the same repository source
RUN apk add --no-cache nginx nginx-mod-http-headers-more

# Forward request logs to docker log collector
RUN ln -sf /dev/stdout /var/log/nginx/access.log && ln -sf /dev/stderr /var/log/nginx/error.log

# Copy the compiled .js file from the build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

#HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
#  CMD wget -qO- http://localhost/ >/dev/null || exit 1

CMD ["nginx", "-g", "daemon off;"]





