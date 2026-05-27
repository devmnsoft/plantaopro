# Execução local - PlantãoPro

## URLs padrão de desenvolvimento

- **API (`PlantaoPro.Api`)**
  - HTTPS: `https://localhost:51977`
  - HTTP: `http://localhost:51978`
  - Página inicial: redireciona para `/swagger` em Development
  - Health check: `/api/health`
- **Web (`PlantaoPro.Web`)**
  - HTTPS: `https://localhost:58285`
  - HTTP: `http://localhost:58286`
  - Página inicial de execução: `/Account/Login`

## Opção 1 — Rodar API + Web juntos (recomendado)

No Visual Studio:

1. Clique com botão direito na **Solution**.
2. Abra **Configure Startup Projects**.
3. Selecione **Multiple startup projects**.
4. Configure:
   - `PlantaoPro.Api` = `Start`
   - `PlantaoPro.Web` = `Start`

Depois de iniciar:

- Abra o Web em `https://localhost:58285/Account/Login`.
- Swagger da API em `https://localhost:51977/swagger`.

## Opção 2 — Rodar somente a API

1. Defina `PlantaoPro.Api` como startup.
2. Inicie o projeto.
3. Abra `https://localhost:51977/swagger`.

## Opção 3 — Rodar somente o Web

1. Defina `PlantaoPro.Web` como startup.
2. Garanta que a API esteja em execução separadamente em `https://localhost:51977`.
3. Inicie o Web e abra `https://localhost:58285/Account/Login`.

## Observações

- O navegador principal deve abrir a interface **Web**, não a raiz da API.
- O Web consome a API via configuração `PlantaoProApi:BaseUrl`.
