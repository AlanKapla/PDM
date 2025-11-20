# 1. Stage build – budowanie aplikacji React
FROM node:20-alpine AS build

# Katalog roboczy wewnątrz kontenera
WORKDIR /app

# Najpierw kopiujemy pliki z zależnościami,
# żeby cache builda lepiej działał
COPY package*.json ./

# Jeśli używasz npm:
RUN npm ci

# Jeśli używasz yarn, zamień powyższe na:
# COPY yarn.lock ./
# RUN yarn install --frozen-lockfile

# Kopiujemy resztę źródeł
COPY . .

# Build produkcyjny
RUN npm run build
# albo przy yarn:
# RUN yarn build


# 2. Stage runtime – serwowanie statycznych plików przez nginx
FROM nginx:alpine AS runtime

# Usuwamy defaultową konfigurację (opcjonalnie)
RUN rm /etc/nginx/conf.d/default.conf

# Prosta konfiguracja nginx do serwowania SPA
# (obsługa routingu client-side: zawsze index.html)
COPY <<EOF /etc/nginx/conf.d/default.conf
server {
    listen 80;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files \$uri /index.html;
    }
}
EOF

# Kopiujemy zbudowany frontend z pierwszego stage
COPY --from=build /app/build /usr/share/nginx/html

# nginx nasłuchuje na 80 w kontenerze
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
