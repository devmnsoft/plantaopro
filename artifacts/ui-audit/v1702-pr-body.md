## Build
- Restore, build e testes .NET executados na validação final (ver resultado do PR).

## Testes
- Testes do backend e gates estáticos executados conforme disponibilidade do ambiente.

## Scripts
- Smoke e gate de layout migrados para o contrato v1.70.2.

## Mobile
- Lint, typecheck e testes executados conforme disponibilidade das dependências.

## Runtime
- O smoke exige aplicação/API ativas e storage state autenticado; não há aprovação simulada.

## Screenshots
- Quando executado, o runner salva em `artifacts/ui-audit/screenshots/v1702/`.

## Notificações
- Mantida a integração real, estados específicos, renderização via DOM seguro e destino same-origin.

## Minha Assinatura/Billing
- Mantido `GET api/minha-assinatura`, com campos reais e empty state honesto.

## BFF
- Recupera chaves atuais/legadas, retorna 401 sem token, preserva request e não devolve exceções ao cliente.

## Admin SaaS
- Mantidos atalhos reais e estados vazios; sem plano, limite, uso ou billing estimado.

## Relatórios
- Somente actions existentes são acionáveis; recursos dependentes de backend ficam desabilitados com motivo.

## Configurações
- Catálogo aponta para rotas implementadas e para Minha Assinatura.

## Smoke v1702
- 22 rotas × 8 viewports. Execute com `PLANTAOPRO_BASE_URL` e `PLANTAOPRO_STORAGE_STATE` via `scripts/ui/run-visual-smoke.sh`.

## Pendências reais
- Homologação visual completa depende de runtime .NET, API, Playwright e credenciais de teste válidas.

## Resgate do PR #339
- A `main` já continha a integração revisada pela v1.70.1 (PR #340). Esta branch preserva essa versão mais recente, reforça o BFF e promove runners, gates e evidências para v1.70.2, sem reaplicar código obsoleto sobre a implementação atual.
