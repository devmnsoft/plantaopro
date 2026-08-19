# Plano de fechamento para produção

## Objetivo e estado encontrado
Estabilizar o produto .NET existente em `backend/` sem alterar o legado da raiz. A inspeção encontrou **PlantaoPro**, e não os projetos/rotas `Valora.*` descritos na solicitação. A solução contém API, Web, camadas Domain/Application/Infrastructure, ferramentas operacionais e testes preexistentes. O SDK .NET não está instalado no ambiente desta execução; portanto build e startup continuam pendentes de validação.

## Ordem, dependências e status
| Fase | Dependência | Status atual | Aceite |
|---|---|---|---|
| P0 Fundação | SDK .NET, PostgreSQL e schema `plantaopro` | Em andamento: JWT Development e rota System Health corrigidos; build bloqueado pelo ambiente | clean/restore/build; API/Web iniciam; banco verificado |
| P1 Identidade/SaaS | P0 validada | Não iniciada | organização, usuários, perfis e plano funcionais |
| P2 Fluxo operacional | P1 | Não iniciada | fluxo produtivo real ponta a ponta |
| P3 Inteligência | evidências reais do P2 | Não iniciada | resultados rastreáveis, sem conteúdo inventado |
| P4 Entregáveis/governança | P3 | Não iniciada | relatórios/exportações reais e auditáveis |
| P5 Enterprise | entitlements e P4 | Não iniciada | integrações reais ou indisponibilidade honesta |
| P6 Go-live/design | fases anteriores estáveis | Não iniciada | publish, segurança, operação e UX validados |
| P8 Testes novos | fluxo produtivo estável | Reservada por último | somente após homologação funcional |

## Riscos
- O nome, domínio e mapa de rotas solicitados não correspondem ao repositório presente; não serão criados módulos Valora paralelos dentro do PlantaoPro.
- `PlantaoPro.Tools.Database install` referencia `database/scrpt_completo.sql`, ausente no checkout. Reconstituir schema sem fonte canônica arriscaria dados reais.
- Sem `dotnet`, não é possível afirmar compilação ou startup.
- Sem PostgreSQL/configuração local, não é possível executar smoke de login ou banco.
