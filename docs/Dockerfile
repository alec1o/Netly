FROM node:20.17.0-alpine

RUN npm install -g pnpm

WORKDIR /app

COPY package.json ./
COPY pnpm-lock.yaml ./

RUN pnpm install

COPY . .

EXPOSE 3000/tcp

ENV PORT=3000

CMD ["pnpm", "serve", "--port", "${PORT}", "--host", "0.0.0.0"]
