FROM node:20

RUN npm install -g pnpm

WORKDIR /app

COPY package.json ./
COPY pnpm-lock.yaml ./

RUN pnpm install

COPY . .

RUN pnpm build || true

EXPOSE 3000/tcp

ENV PORT=3000

CMD ["pnpm", "serve", "--port", "${PORT}", "--host", "0.0.0.0"]
