# C7-A0 — Auditoria inicial do ciclo de conclusão integrada (estado atual COM evidência)

- Data: 2026-10-10 · Base auditada: `HEAD = 0d6b2d2` (= `origin/main`, push confirmado) · árvore limpa.
- Método: documentos históricos (relatório maio, doc junho, anexos) usados apenas como ponteiros; cada linha abaixo foi
  reverificada neste ciclo no HEAD (código, migrations e execução da suíte). Nada declarado "pronto" sem evidência apontável.

## 1. Stack e base confirmadas

| Item | Evidência |
|---|---|
| Solução oficial | `backend/PlantaoPro.sln`, SDK net10.0 (README §instalação + build executado neste ciclo) |
| Arquitetura | ASP.NET Core MVC/Razor (Web BFF) + controllers API · PostgreSQL (local 127.0.0.1:5432) · Dapper |
| CI | `.github/workflows/dotnet-ci.yml` (GitHub Actions) |
| Banco canônico | instalação limpa por `database/install-manifest.json`; upgrades por `migration-manifest.json` via `scripts/apply-canonical-migrations.sh upgrade` (README v1.18.5) |
| Build fresco (este ciclo) | 0 erros na solução completa |
| Suíte | 1204 testes. Rodada 1 deste ciclo: 1203 aprovados + 1 falha isolada (nome não capturado, não reproduzida). Rodada 2: **1204/1204 aprovados** (47 s). Observação registrada: instabilidade isolada a monitorar |

## 2. Matriz obrigatória — Recurso → Evidência → Estado → Problema → Ação → Validação

Estados: **pronto** (com evidência) · **parcial** · **quebrado** · **ausente** (após busca) · **não verificado**

### Ponto 1 — Estabilizar a base

| Recurso | Evidência (HEAD) | Estado | Problema atual | Ação | Validação |
|---|---|---|---|---|---|
| Build dos projetos afetados | `dotnet build PlantaoPro.sln` executado no ciclo: 0 erros | pronto | — | manter | CI + build local por entrega |
| Login → sessão → home → logout | `AccountController`: redirect por perfil (`RedirectToActionByPerfil` L245-294, tabela de prioridade), `NoEligibleContext` (L138), `returnUrl` só se local (`Url.IsLocalUrl` L467), landing resiliente (destino não contratado → `MeuDia`, não AccessDenied pós-login) | pronto | — | revisão de telas no bloco design | e2e `web-bff` (suíte) |
| Rotas únicas (histórico: duplicidade de disponibilidade) | `Fase4OperationalController.cs`: `api/medicos/me/disponibilidade` (GET/POST/{id} PUT/DELETE), `api/medicos/me/indisponibilidade`, `api/coordenacao/medicos-disponiveis` — caminhos distintos, sem sobreposição método+caminho | pronto | nenhuma duplicidade encontrada na investigação | monitorar em novas rotas | Swagger + contract de rotas |
| DTOs/Dapper/paginação/listas | suíte de integração contra PostgreSQL real (milhares de chamadas por jornada) | pronto | sem falhas ativas observadas | — | suíte 1204 |
| Divergências colunas/tipos SQL | manifests de instalação/upgrade + v2339/v2340 aplicados limpos em `plantaopro_test` neste ciclo | pronto | — | — | upgrade reexecutável (contrato do migrador) |
| Meu Dia (histórico: timeout) | `MeuDiaServices.cs` dados reais + `Saude360R4B9PlantoesMeuDiaBiTests` (incl. isolamento de tenant) | pronto | — | — | suíte |
| Erros preservando valores do formulário / sucesso só após persistência | coberto por e2e de jornadas (web-bff) nos fluxos trabalhados | pronto (por jornada) | telas fora das jornadas auditadas não varridas sistematicamente | varrer no bloco design | e2e |

### Ponto 2 — Tenants, perfis, planos e menus

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Autorização central no servidor (usuário ativo + tenant + vínculo + permissão + módulo contratado + assinatura) | R6-A1/A1b: `SaasRouteGuardFilter` (live-check autoritativo + motivo), funções canônicas `modulos_efetivos`/`modulo_efetivo` (v2339/v2340), motivo `VERIFICACAO_INDISPONIVEL` com retry, caminho único por claims, API por-request (`Saude360ModuleFilter` fail-closed), motivos separados (não contratado / permissão / verificação indisponível / cliente bloqueado) | pronto | DECs pendentes: filtro leniente `is null or` de relatórios p/ linhas sem tenant; UNIDADES fina comercial; contagem P3 `ManagerCommandCenterService` | decidir (registro em `r6a1b` §8) | testes `R6A1*`, `R6A1b*`, `F4` (suíte) |
| Isolamento entre tenants | múltiplos e2e (ex.: `Aceite03_IsolamentoMultiTenant...`, triagem F1b, `MeuDiaMedico_...IsolamentoDeTenant`) | pronto | — | — | suíte |
| Plano/pacote/capacidade efetivos + menu coerente | v2339/v2340 + endpoint `effective-modules` + menu alinhado ao guard (R6-A1) | pronto | — | — | suíte + S1/S2/S3 provas de banco |
| Diferença Plano do sistema × Plano de saúde × Convênio | modelo separa os três (catálogo SaaS distinto de `planos_saude`/convenios clínicos) | parcial | terminologia nas telas não revisada (botões/textos podem confundir) | bloco design/textos + i18n | walkthrough de telas + contrato de textos |
| Home por perfil (não a mesma para todos) | tabela de prioridade por perfil (AdminSaas, ClientePortal, CentralEscala, Financeiro, MedicoArea, HospitalArea, CentralAtendimento, Triagem/Fila, ParceiroPortal, Suporte, Auditoria, Comercial, CustomerSuccess) com fallback resiliente | pronto | — | — | e2e |
| Menus por jornada/plano/perfil | sidebar em seções (Plantões, Saúde 360, Administrativo 360, Financeiro, Configurações...) derivada do catálogo/guard | pronto | ajuste fino visual pendente | bloco design | contrato de menu × guard |
| Onboarding persistente e retomável | `OnboardingJornadaService`: catálogo ONB_*, conclusão **derivada de dados no banco** (reevaluidor), etapa obrigatória nunca pulada (409), Iniciar/Sincronizar/Reavaliar/Concluir/Pular/Restaurar, escopo por módulos contratados | pronto | passo de preferências (idioma/identidade visual) depende de i18n | concluir no corte i18n | `OnboardingC7Tests` + novo teste do passo |

### Ponto 3 — Plantões ponta a ponta

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Máquina de estados/consolidação | `PlantaoTransicaoService` + contrato de migração `V2157OperationalCycleContractTests` (proteção vaga/convite/escala/pagamento sob concorrência) | pronto | — | — | suíte |
| Dois aceites na última vaga → uma confirmação | mecanismo em `Data.cs` L1455-1510: `FOR UPDATE` na linha do plantão, revalidação de vagas/status **sob lock**, intervalo `[inicio, fim)` (fim exclusivo), elegibilidade revalidada, idempotência (já aceito → mesmo escala), UniqueViolation → 409 | parcial | **teste comportamental dedicado da corrida da última vaga ausente após busca** | criar teste de concorrência real (dois aceites simultâneos) | novo teste de integração |
| Substituição preserva escala original até confirmação | fluxo completo com `VersaoEsperada` (lock otimista), aprovar/recusar com justificativa, convite/confirmar substituto, histórico, guard de vínculo/hospital | pronto | — | — | suíte |
| Financeiro médico (obrigação única, parcial, contestação, estorno/rastreável) | R4-B7/B8 (cancelamento/contestação, fechamento de caixa, origem da escala) | pronto | — | — | suíte |
| **Unificação de conflitos** (escalas × agenda clínica × bloqueios × indisponibilidades) | escala×escala ✓ (revalidação no aceite); agendamento×agendamento ✓ (`Saude360ClinicalService.cs:144`, intervalo fim-exclusivo, escopo tenant); **escala×agendamento ✗** (criação de agendamento não verifica escalas confirmadas); bloqueio/indisponibilidade do médico ✗ (não checados na criação do agendamento) | parcial | conflito impeditivo escala↔consulta pode passar despercebido | cruzar conflitos na criação/reagendamento de agendamento + diferenciar alerta de solicitação pendente | teste: consulta sobreposta a escala confirmada → 409 identificado (linha da matriz de aceite) |

### Ponto 4 — Administrativo 360

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Solicitação → aprovação → compra → recebimento → estoque → financeiro | série G + `Integracao_AceiteNumerico_JornadaCompleta7Passos` (estoque/orçamentos) | pronto | — | — | suíte |
| Nota repetida não duplica efeitos (compra/estoque/lançamento) | `Aceite04_CotacaoRepetida_NaoDuplica`, `Aceite12/15` (XML), outbox com retries (`Aceite09`) | pronto | — | — | suíte |
| Recebimento parcial / saldo; transferência consistente; ajuste com motivo+permissão; reversão compensatória | testes `Administrativo360EstoqueEOrcamentosTests` (saldo, reserva acima, orçamento) | pronto | — | — | suíte |
| Bionexo/INPART/SEFAZ | estados honestos; `Aceite11_SemCredenciais_IntegracaoFicaBloq...` — sem sucesso simulado | pronto (estado bloqueado evidenciado) | dependência externa: credenciais | conectar quando houver credenciais (fluxo manual segue funcional) | teste com sandbox |
| Meu Dia / Resumo 360 com dados reais e navegação à operação | P1 do Meu Dia + BI (`Saude360R4B9...`) | pronto | — | — | suíte |

### Ponto 5 — Saúde 360

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Jornada paciente → agendamento → check-in → chamada → triagem → consulta → prescrição → cobrança | engine `Saude360ClinicalService` + e2e de runtime (`HomologacaoCrudsAcoesJornadas`, `HomologacaoRuntimeReal`, F-série, R4) | pronto | — | — | suíte |
| Check-in repetido não cria outro atendimento/fila | mecanismo: `on conflict do nothing` + `not exists` em fila de chamada e triagem (`Saude360ClinicalService.cs` L684-697) | parcial | **teste dedicado "check-in repetido" ausente após busca** | criar teste de idempotência | novo teste |
| Reserva concorrente segura de item de fila (dois operadores) | política `PainelChamada.Operar` (V1182) + mensagem de conflito de reserva (L190) | não verificado | prova de concorrência específica não localizada | inspecionar serviço de chamada e garantir lock/claim atômico + teste | novo teste |
| Painel público com dados mínimos, credencial limitada/revogável | `PainelTvService` + `V1243PainelTvSecurityContractTests` (scan de campos expostos) | pronto (contrato) | revalidação visual pendente | bloco design | contrato + captura |
| CID pesquisável com versão/origem | `cid_tabela` (versão CID-10, fonte, upsert versionado L405) | pronto | — | — | suíte |
| Consulta finalizada → adendo rastreável (sem edição silenciosa) | `consulta_adendos` (hash + motivo + autor) e `consulta_historico` versionado na finalização (L111, L226) | pronto | — | — | suíte |
| Plano de saúde do paciente (distinto do plano SaaS) | `planos_saude` + troca de principal atômica com índice único parcial (v2306, violação → 409) | pronto | terminologia nas telas | bloco design/textos | suíte |

### Ponto 6 — Onboarding, assinatura, administração global

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Assistente retomável por tenant, etapas validadas pelo banco | seção Ponto 2 (Onboarding) | pronto | passo de preferências (idioma) pendente | depende do corte i18n | suíte + teste novo |
| Contratação/aprovação idempotente; faturas sem duplicação; eventos repetidos | B2 (R5) APROVADA idempotente; serviços SaaS com dedupe (suíte SaaS) | pronto | — | — | suíte |
| Bloqueio com motivo e efeitos; desativação não apaga histórico | serviços SaaS (bloqueio com motivo) + `CLIENTE_BLOQUEADO` na autorização | pronto | — | — | suíte |
| Administrador global: gestão completa, contexto explícito para conteúdo clínico sensível, auditoria | R6-A1/A1b (contexto de tenant exigido; 403 com orientação quando ausente) | pronto | — | — | suíte (`F4`, `R6A1b`) |

### Ponto 7 — Homologação integrada (design, idiomas, segurança, operação)

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| i18n: pt-BR/en/es/**fr** (menus, campos, botões, validações, ajuda, relatórios, comunicações) | busca ampla: nenhum sistema de localização implementado | **ausente** | todo o bloco | **corte transversal próximo**: recursos 4 idiomas, preferência persistente do usuário (precedência usuário → organização → browser → pt-BR), sem chaves expostas, formatação data/número/moeda independente da moeda contratada, sem auto-tradução de conteúdo clínico | novo contrato de i18n + testes dos 4 idiomas |
| Design/contraste/formulários/viewport (celular/tablet/desktop) | inventário R5 (antes/depois) existe; sem nova varredura visual neste ciclo | não verificado | revalidação visual de todas as telas das jornadas | bloco design após/parallel com i18n | capturas + checklist |
| CSRF em formulários autenticados por cookie | antiforgery do kit BFF (e2e exigem token) | pronto | — | — | suíte |
| Auditoria durável (transação/outbox, sem descarte silencioso) | histórico transacional por comando + outbox p/ notificações | pronto | — | — | suíte |
| IA restrita a assistência operacional (sem conceder acesso/pagamento/nota/decisão clínica) | `AiRodada2GovernancaTests` (cotas, slots com concorrência, autoexpiração) | pronto | respostas no idioma escolhido quando i18n existir | alinhar ao corte i18n | suíte |
| Health checks / observabilidade restrita; backup/restore documentado | health checks padrão presentes; documentação de backup segue README/infra | não verificado | validar configuração efetiva | checklist operacional (janela do usuário) | verificação manual |
| Homologação viva (host/produção representativa) + upgrade formal do banco principal `plantaopro` (v2339+v2340 hoje só em `plantaopro_test`) + IIS | aplicados em `plantaopro_test`; principal ainda sem upgrade formal | bloqueado (externo) | janela do usuário | roteiro de aceite em `r6a1b` §7 + upgrade via manifest | execução assistida |

## 3. GATE (seção 5 do prompt) — situação local

| Verificação | Status |
|---|---|
| Build dos projetos afetados | ✅ (0 erros, este ciclo) |
| Login e sessão funcionais | ✅ (e2e web-bff na suíte) |
| Rotas sem duplicidade | ✅ (investigado; nenhuma encontrada) |
| Acesso permitido/negado por perfil | ✅ (guard + motivos, testes dedicados) |
| Isolamento entre dois tenants | ✅ (vários e2e) |
| Migrations aplicadas em ambiente descartável | ✅ (v2339/v2340 em `plantaopro_test`) |
| Gravação e releitura de registros básicos | ✅ (suíte de integração) |

**GATE satisfatório localmente.** Fluxos dependentes liberados para avançar. A homologação *viva* (host do usuário) permanece etapa separada e pendente de janela.

## 4. Ordem de cortes proposta (honesta, por prioridade do prompt)

1. **R7-A1 — Plantões: lacunas de conflito e provas de concorrência** (menor e de maior risco):
   - cruzar conflito escala confirmada × criação/reagendamento de agendamento (409 impeditivo) + bloqueio/indisponibilidade do médico;
   - teste de corrida da última vaga (dois aceites simultâneos → uma confirmação);
   - teste de check-in repetido (idempotência);
   - inspeção/prova de reserva concorrente de item de fila.
2. **R7-E1 — Fundação i18n (pt-BR/en/es/fr)** como infraestrutura transversal (habilita passo de preferências do onboarding e o bloco de textos): recursos, preferência persistida com precedência, `<html lang>`, formatação, zero chaves expostas, não-alterar moeda/tenant.
3. **R7-OB — passo de preferências (idioma/identidade visual) no onboarding** (depende de E1).
4. **R7-D — Design/textos por jornada** (termos "plano do sistema" × "plano de saúde", agendamento/atendimento/consulta, botões verbais, estados vazios, "Como usar", relatórios com período/filtros/definições).
5. **Externo/DEC:** UNIDADES fina comercial, conector fiscal (credenciais), upgrade formal do banco principal + IIS, homologação viva (roteiro `r6a1b` §7).

## 5. Notas

- Falha isolada da suíte (roda 1 deste ciclo): nome não capturado (saída truncada); reexecução completa verde 1204/1204. Registrar e investigar se reincidir.
- Linhas "pronto" referem-se ao comportamento comprovado pela suíte de integração contra PostgreSQL real — não a validação visual, que é do ponto 7.
