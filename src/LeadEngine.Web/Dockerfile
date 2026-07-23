FROM node:24-alpine AS build
WORKDIR /app
COPY src/LeadEngine.Web/package*.json ./
RUN npm install
COPY src/LeadEngine.Web .
ARG VITE_API_BASE_URL=http://localhost:5080
ARG VITE_CORRETOR_WHATSAPP=
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_CORRETOR_WHATSAPP=$VITE_CORRETOR_WHATSAPP
RUN npm run build

FROM nginx:1.27-alpine
COPY src/LeadEngine.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
