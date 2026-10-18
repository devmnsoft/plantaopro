# Arquitetura

DDD + Clean Architecture com separação Domain, Application, Infrastructure, Api e Web.

## Mapa dos contratos de casa

Contratos transversais que valem para qualquer slice novo — cada um com ADR ou guarda de fonte versionada:

| Contrato | Onde vive | Referência |
|---|---|---|
| Binding Dapper/Npgsql p/ datas (records: `DateTime` no construtor) | `adr/` + `R19DapperBindingGuardTests` | [ADR-0001](adr/adr-0001-contrato-binding-dapper-npgsql-datas.md) |
| Funções set-returning só em posição FROM (PG18) | `adr/` + `R19DapperBindingGuardTests` | [ADR-0002](adr/adr-0002-set-returning-functions-pg18.md) |
| Espelho BFF ViewModel × DTO (grafia idêntica à chave do wire) | `adr/` + `R20BffMappingContratoTests` | [ADR-0003](adr/adr-0003-bff-contrato-espelho-viewmodel-dto.md) |
| Catálogo canônico de módulos (`plantaopro.modulos_efetivos`) | `CrossCutting/ModuleContractVigencia.cs` | R6 / R18 (guarda de posição SRF) |
| Guard de rotas por módulo contratado (audience × pacote) | `Web/Services/Security/SaasRouteGuardFilter.cs` | R17 / R18 (dicionário + overrides) |
| i18n Adm360 via `UiText.L` + resx ×4 | `PlantaoPro.Web` (suíte de contrato R7DF4/R7DF6) | 653 chaves estáveis |
| Compatibilidade legada do catálogo | raiz de `arquitetura/` | [catalogo-compatibilidade-legada.md](catalogo-compatibilidade-legada.md) |

## Decisões de arquitetura (ADR)

- [ADR-0001 — Contrato de binding Dapper/Npgsql para datas](adr/adr-0001-contrato-binding-dapper-npgsql-datas.md)
- [ADR-0002 — Posicionamento de funções set-returning no PostgreSQL 18](adr/adr-0002-set-returning-functions-pg18.md)
- [ADR-0003 — Contrato BFF: espelho ViewModel × DTO da Api (família Administrativo360)](adr/adr-0003-bff-contrato-espelho-viewmodel-dto.md)

Regra de manutenção: decisão de contrato tomada em rodada vira ADR (aceita, com contexto/decisão/consequências) e entra na tabela acima; guardas automáticas preferidas a convenção não verificada.

## Direcionadores

- **Banco:** Postgres 18, schema real `plantaopro` (sem sublinhado); tabela por agregado com `reg_status`/versão otimista; migrações numeradas em `backend/**/Migrations`.
- **API × Web:** BFF (Web) consome a API via HTTP/JSON; tipos de fronteira JSON podem usar `DateTimeOffset`, tipos ligados ao banco não (ADR-0001).
- **Gate:** build Release `--no-incremental` 0 erros / 80 avisos com multiset idêntico ao pristino; suíte versionada em `docs/evidencias/<data>-rodada-N/`.
