FROM node:24.14.0 as build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install --legacy-peer-deps
COPY  . .
RUN npm run build


FROM nginx:alpine
COPY  --from=build /app/dist/Frontend/browser /usr/share/nginx/html
EXPOSE 80
