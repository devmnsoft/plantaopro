# Plano de fechamento para produção

## Objetivo e estado encontrado
Estabilizar o PlantãoPro .NET existente em `backend/`, preservando a solução, suas camadas e os testes preexistentes. O SDK .NET não está instalado neste ambiente de manutenção; build e startup permanecem dependentes do workflow com SDK .NET 10.

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
- Somente rotas e módulos PlantãoPro comprovados no código fazem parte do fechamento; contratos fictícios não serão criados.
- `PlantaoPro.Tools.Database install` referencia `database/scrpt_completo.sql`, ausente no checkout. Reconstituir schema sem fonte canônica arriscaria dados reais.
- Sem `dotnet`, não é possível afirmar compilação ou startup.
- Sem PostgreSQL/configuração local, não é possível executar smoke de login ou banco.

## Sprint corretiva v1.95.1 (2026-08-20)

A v1.95.1 corrige os bloqueios de compilação, reconcilia pagamentos de plantões e ordena as fontes v1.41/v1.87 na cadeia de upgrade. A conclusão de produção continua condicionada aos gates executáveis de build, banco PostgreSQL 16, runtime, autenticação e isolamento tenant no CI; documentação não substitui essas evidências.
