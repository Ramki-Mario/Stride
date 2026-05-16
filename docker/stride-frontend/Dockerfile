# ── Stage 1: build Angular SPA ────────────────────────────────────────────────
FROM node:22-alpine AS builder

WORKDIR /app

# Copy manifests first so npm install layer is cached unless deps change.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --prefer-offline

COPY frontend/ ./
RUN npm run build -- --configuration=production

# ── Stage 2: serve with nginx + BFF proxy ─────────────────────────────────────
FROM nginx:1.27-alpine

# Remove default nginx config.
RUN rm /etc/nginx/conf.d/default.conf

# Copy the production nginx config that proxies /bff/* to STRIDE.BFF.
# This replicates what proxy.conf.json does during `ng serve` in development.
COPY infrastructure/nginx/nginx.conf /etc/nginx/nginx.conf

# Copy compiled Angular output from Stage 1.
COPY --from=builder /app/dist/stride-frontend/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
