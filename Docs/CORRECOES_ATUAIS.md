# BarberSync SaaS Platform Demo 10.0 — CORRECOES ATUAIS

## Objetivo
Consolidar uma demonstração comercial estável para salões, barbearias, estética e franquias, cobrindo API, AdminWeb, PublicWeb, KioskWeb, MobileApp, proxies MVC e fallback demo.

## Pontos validados nesta etapa
- Navegador usa somente proxies locais: /AdminApi, /PublicApi e /KioskApi.
- API mantém endpoints essenciais de Swagger, dashboard, serviços, profissionais e totem.
- AdminWeb possui rotas principais, cards, tabelas demo, ações CRUD visuais, DemoStore em localStorage e EventBus.
- PublicWeb lista serviços/profissionais e gera protocolo de agendamento demo.
- KioskWeb percorre serviço, cliente, profissional, confirmação, pagamento mock, sucesso e avaliação com sessionStorage.
- Docker Compose define API, AdminWeb, PublicWeb, KioskWeb e Seq.

## Roteiro rápido
1. Subir a stack com docker compose up -d.
2. Abrir Swagger em http://localhost:8080/swagger.
3. Apresentar Admin em http://localhost:8081/Admin.
4. Converter lead/agendamento em http://localhost:8082/.
5. Fechar fluxo de autoatendimento em http://localhost:8083/Kiosk/Services.

## Pendências reais
- Validar em ambiente com SDK .NET e Docker disponíveis.
- Conectar aos módulos persistidos definitivos quando a base BarberSync de produção for liberada.
