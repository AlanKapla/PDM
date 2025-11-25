# 1. Stage build – Vite
FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Vite build -> /app/dist
RUN npm run build


# 2. Stage runtime – nginx
FROM nginx:alpine AS runtime

RUN rm /etc/nginx/conf.d/default.conf

# Prosty config dla SPA – tylko dla UI
COPY <<EOF /etc/nginx/conf.d/default.conf
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files \$uri \$uri/ /index.html;
    }
}
EOF

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]