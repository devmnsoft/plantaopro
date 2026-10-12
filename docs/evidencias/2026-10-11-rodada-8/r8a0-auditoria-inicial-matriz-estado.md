# R8-A0 — Auditoria inicial da rodada 8 (estado atual COM evidência)

- Data: 2026-10-11 · Base auditada: `HEAD = 2f4533d3` = `origin/main` (**push verificado** via `git ls-remote`). Árvore limpa no momento da auditoria; esta rodada aplicou **correções pontuais R8-A1** (commit `cedd209`, seção 6).
- Prompt de referência: evolução integrada expandida (seções 1–16 + complemento 17–30).
- Método: cada linha abaixo foi verificada neste ciclo contra o HEAD (código, migrations, testes e execução de suíte). Nada é declarado pronto sem evidência apontável. Os documentos históricos e da rodada 7 (`c7a0-auditoria-inicial-matriz-estado.md`) servem de base e foram **reconfirmados**, não copiados.

## 0. Registro de entregas entre auditorias (0d6b2d2 → 2f4533d3)

| Commit | Conteúdo | Evidência |
|---|---|---|
| `3adfc55` | C7-A0 auditoria inicial rodada 7 | `docs/evidencias/2026-10-10-rodada-7/c7a0-…md` |
| `9588fab` | R7-A1 conflito unificado do profissional + provas de concorrência (migrations **v2341–v2343**) | `c7a1-plantaes-conflitos-unificados.md` · `R7A1PlantoesConflitosUnificadosTests` (15 testes) |
| `2f4533d3` ("novos") | R7-E1 fundação i18n pt-BR/en/es/fr: `UiLanguageCatalog`, claims `idioma`/`org_idioma`, `usuarios.idioma_preferido` (**v2344**), `POST api/usuarios/me/idioma` com reemissão de sessão, `SessionReissueService`, provider de cultura, resx ×4 (60 chaves), 4 views + `<html lang>`; **inclui os 3 contratos de fonte atualizados** (V2150/V2156/V2164) | `r7e1-i18n-fundacao.md` · `R7E1I18nFundacaoTests` (29 testes) · conteúdo do commit conferido (`git show 2f4533d`) |

Nota: o commit `2f4533d3` foi criado e empurrado externamente ao agente (mensagem "novos"). Seu conteúdo é exatamente o pacote R7-E1 auditado; a mensagem segue como está (reescrita de commit já publicado evitada).

## 1. Stack e base confirmadas (revisão 10/10 → 11/10)

| Item | Evidência |
|---|---|
| Solução oficial | `backend/PlantaoPro.sln`, SDK net10.0 |
| Arquitetura | Web BFF MVC/Razor + controllers API · PostgreSQL 18 local (serviço `postgresql-x64-18`) · Dapper 2.1.35 / Npgsql 10.0.2 |
| Migrations canônicas | até **v2344** em `database/migrations/` + `migration-manifest.json`; v2339→v2344 aplicadas em `plantaopro_test`; banco principal `plantaopro` **sem upgrade formal acumulado** (pendência externa) |
| Build fresco (11/10, build10) | `dotnet build PlantaoPro.Tests.csproj -nodeReuse:false` → **0 erros** / 55 avisos (pré-existentes, sem novos bloqueantes); `PlantaoPro.Tests.dll` regenerado 11/10 08:11 |
| Suíte | 1248 testes (1219 pós-R7-A1 + 29 R7E1). Execução v2 em curso nesta rodada: gate contratos+R7E1+R7A1 (≈47) + suíte completa ×3 (logs `Temp\opencode\v2-*.log`, resumo central `suite-final2-summary.txt`) |

## 2. Matriz obrigatória — frente por frente (estado pós-R7-E1)

Estados: **pronto** (com evidência) · **parcial** · **quebrado** · **ausente** (após busca) · **não verificado**

### Frente 1 — Configuração operacional, planos/perfis/navegação, onboarding (§4, §5, §12)

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Autorização central no servidor (usuário + tenant + unidade + permissão + módulo contratado + assinatura) | R6-A1/A1b: `SaasRouteGuardFilter` live-check com motivo; funções canônicas `modulos_efetivos`/`modulo_efetivo` (v2339/v2340); motivos separados (não contratado/permissão/verificação indisponível/cliente bloqueado) | pronto | DECs pendentes (filtro leniente de relatórios p/ linhas sem tenant; UNIDADES fina comercial; contagem P3) | decidir | testes `R6A1*`, `F4` |
| Isolamento entre tenants | múltiplos e2e (`Aceite03_IsolamentoMultiTenant…`, triagem F1b, `MeuDiaMedico_…IsolamentoDeTenant`) | pronto | — | — | suíte |
| Plano/pacote/capacidade efetivos + menu coerente | v2339/v2340 + `effective-modules` + guard | pronto | catálogo único §5 (recurso→módulo→plano→limite→permissão→rota→menu→estado) ainda não consolidado como artefato consultável | varredura §5 (agente desta rodada) | contrato menu×guard |
| Diferença Plano do sistema × Plano de saúde × Convênio | modelos separados (`catálogo SaaS` × `planos_saude`/convênios) | parcial | terminologia nas telas não revisada | R7-D (textos) | walkthrough + contrato de textos |
| Home por perfil (página inicial específica) | tabela de prioridade `RedirectToActionByPerfil` (13 perfis) com fallback resiliente | pronto | — | revisão visual | e2e |
| Menus por jornada/plano/perfil | sidebar em seções derivada do catálogo/guard | pronto | ajuste fino visual pendente | R7-D | contrato menu×guard |
| Configuração consolidada p/ admin do cliente (finalidade/efeito/quém muda; histórico; desativação com vínculos) | telas de Configurações existentes por domínio; sem inventário sistêmico dos metadados "finalidade/efeito/responsável" nem histórico de mudanças de configuração | parcial (varredura em curso) | §4 não atende integralmente: efeito/histórico/desativação-com-vínculos não padronizados | varredura §4 (agente) → fatia R8-config | suíte + walkthrough |
| Onboarding retomável por etapas validadas no banco | `OnboardingJornadaService` (ONB_*, conclusão derivada de dados, 409 etapa obrigatória, escopo por módulos) | **pronto com evidência (R7-OB desta rodada)** | etapa "Definir o idioma da interface" (v2345) entregue: opcional de núcleo, critério derivado de dados (padrão da organização ou preferência do usuário) | **entregue** — `r7ob-onboarding-preferencias-idioma.md` | `OnboardingC7Tests` + `R7OBOnboardingPreferenciasIdiomaTests` (5 fatos; gate 52/52) |

### Frente 2 — Plantões: planejamento/cobertura e execução/remuneração (§6, §7)

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Máquina de estados de plantões | `PlantaoTransicaoService` + contrato `V2157OperationalCycleContractTests` | pronto | — | — | suíte |
| Conflito unificado escala × agenda × indisponibilidade (409 impeditivo, fim-exclusivo, política única) | R7-A1: `ConflitoHorarioService.ListarFontesConflitoAsync` (`BusinessRulesServices.cs`); contratada a ausência de implementação paralela | pronto | — | — | `R7A1…` itens 1-6, 15 |
| Corrida da última vaga: dois aceites → uma confirmação | R7-A1 teste 7 (corrida real `Task.WhenAll`; vagas terminam em 0; 1 escala) | pronto | — | — | suíte (linha da matriz de aceite §15) |
| Escala × aceite × agenda clínica | R7-A1: `AceitarConviteAsync` usa política canônica (removida a SQL inline duplicada); teste 8 | pronto | — | — | suíte |
| Bloqueio/indisponibilidade na criação/reagendamento | R7-A1: `medicos.bloqueado` + `medico_indisponibilidades` → 409 (testes 3-4); reagendamento revalida (teste 6) | pronto | — | — | suíte |
| Check-in repetido → um atendimento; fila sem duplicar | R7-A1 testes 9-10 (sequencial 409 + corrida real) | pronto | — | — | suíte (linha §15) |
| Reserva concorrente de item de fila (dois operadores) | R7-A1: guard de estado esperado em `chamar/rechamar/finalizar` + teste 11 | pronto | — | — | suíte |
| Substituição preserva compromisso até confirmação válida | fluxo com `VersaoEsperada`, aprovar/recusar com justificativa, histórico | pronto | — | — | suíte |
| Remuneração: obrigação única, parcial, contestação, estorno rastreável, sem confirmação bancária simulada | R4-B7/B8 (fechamento, origem da escala, cancelamento/contestação) | pronto | fórmula de remuneração versionada/exposta ao médico ainda não auditada nesta linha | confirmar em fatia de plantões | suíte |
| Planejamento recorrente com exceções / visões futuras | recorrência de plantões existente (módulo escalas); exceções de série não auditadas | não verificado | §6 exige recorrência+exceções+ocorrências rastreáveis | varredura dedicada | novo teste se implementado |
| Decisão de produto pendente: semântica de `solicitado` (bloqueia vs alerta) | registrada em `c7a1` §7 | pendente (DEC) | — | deliberar | — |

### Frente 3 — Saúde 360: agenda/fila/atendimento e documentação clínica (§8, §9)

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Jornada paciente→agenda→check-in→chamada→triagem→consulta→prescrição→cobrança | `Saude360ClinicalService` + e2e runtime (Homologação…, F-série, R4) | pronto | — | — | suíte |
| Agendamento ≠ atendimento ≠ chamada ≠ consulta (registros distintos vinculados) | tabelas/estados separados na engine; check-ins vinculados | pronto | — | — | suíte |
| Reserva temporária com expiração / oferta ≠ confirmação | política `PainelChamada` (V1182); oferta de horários liberados não auditada | parcial | varrer estados de oferta/reserva | varredura dedicada | novo teste se aplicável |
| Reagendamento mantém histórico; cancelamento trata fila/reserva/cobrança | `AtualizarAsync` com revalidação de conflito (R7-A1); histórico de agendamento existente | parcial | matriz de efeitos do cancelamento por estágio não confirmada | varredura §8/§21 | suíte |
| CID com código/descrição/versão/origem | `cid_tabela` CID-10 versionado (upsert L405) | pronto | — | — | suíte |
| Consulta finalizada → adendo rastreável, sem edição silenciosa; reabertura c/ permissão+motivo | `consulta_adendos` (hash+motivo+autor) + `consulta_historico` versionado | pronto | reabertura formal não auditada | confirmar na fatia clínica | suíte |
| Proteção contra sobrescrita concorrente de documento clínico | guard de versão em consulta/triagem (padrão de estado esperado estendido pela R7-A1) | pronto | — | — | suíte |
| Painel público mínimo autorizado | `PainelTvService` + `V1243PainelTvSecurityContractTests` (scan de campos) | pronto (contrato) | revalidação visual pendente | R7-D | contrato + captura |
| Plano de saúde do paciente (≠ plano SaaS) | `planos_saude` + troca atômica (v2306, 409) | pronto | terminologia nas telas | R7-D | suíte |

### Frente 4 — Administrativo 360 (compras/estoque) e financeiro (§10, §11, §22)

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Requisição→aprovação→compra→recebimento→estoque→financeiro | série G + `Integracao_AceiteNumerico_JornadaCompleta7Passos` | pronto | — | — | suíte |
| Nota repetida não duplica (compra/estoque/lançamento) | `Aceite04_CotacaoRepetida_NaoDuplica`, `Aceite12/15` (XML), outbox com retries | pronto | — | — | suíte |
| Recebimento parcial; reserva; transferência; ajuste com motivo+permissão; reversão compensatória | `Administrativo360EstoqueEOrcamentosTests` | pronto | — | — | suíte |
| Bionexo/INPART/SEFAZ — estado real, sem sucesso simulado | `Aceite11_SemCredenciais_IntegracaoFicaBloq…` | pronto (bloqueio honesto) | credenciais externas | fila externa | sandbox quando houver |
| Separação financeira: pagamentos médicos / receitas clínicas / convênios / despesas / faturas SaaS | domínios distintos (R4-B7/B8 canonicidade: pagamento de plantão ≠ financeiro clínico, ver obsoletos de `V115`) | pronto | — | — | suíte (`Saude360R4B8FinanceiroCanonicidadeTests`) |
| Obrigação × liquidação × caixa; parcial/desconto/estorno; competência × caixa nos relatórios | R4-B7/B8 + relatórios financeiros existentes; definição de precisão/fechamento registrada | parcial | reconciliação relatório↔origem não varrida sistemicamente nesta rodada | varredura §11/§27 | suíte + prova de reconciliation |
| Convênios: lote→envio→retorno→glosa→recurso | ver linhas §22 (abaixo, agente 3) | aguardando relatório | — | — | — |

### Frente 5 — Design, idiomas e administração global (§13, §14, §12-admin)

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Fundação i18n pt-BR/en/es/fr (precedência usuário→org→browser→pt-BR; sem chave exposta; `<html lang>`; formatação) | **R7-E1** (commit 2f4533d3): `UiLanguageCatalog`, claims, `idioma_preferido` (v2344), reemissão de sessão imediata, provider CurrentCulture=pt-BR constante / UICulture variável, resx ×4 (60 chaves) | **parcial (fundação)** | escopo desta fatia = chrome + `Account/Login` + `Usuario/Index` + 4 views; restante do app = fatia seguinte | **R7-D** varredura de textos/i18n por jornada | `R7E1I18nFundacaoTests` (29) + validação v2 em curso |
| Troca de idioma persistida e efetiva sem relogin | `POST api/usuarios/me/idioma` → `refresh-context` → novas claims | pronto | — | — | teste R7E1 (matriz §15: "Troca de idioma → preferência persistida") |
| Design: contraste/formulários/viewport/estados vazios/"Como usar"/teclado | inventário R5 existe; sem nova varredura completa | não verificado | revalidação visual das jornadas | R7-D | capturas + checklist |
| Administração global: clientes/planos/assinaturas/faturas/suporte/bloqueios; vigência de mudança de plano; downgrade; dedupe de eventos | serviços SaaS (R5-B2 idempotência APROVADA; bloqueio com motivo; `CLIENTE_BLOQUEADO`) | pronto | vigência/downgrade e tratamento de uso excedente não auditados nesta linha | varredura §12 | suíte SaaS |
| IA restrita (sem acesso/pagamento/nota/decisão clínica) | `AiRodada2GovernancaTests` | pronto | respostas no idioma escolhido | alinhar no R7-D | suíte |

### Complemento §17–§30 (novas seções do prompt — varreduras desta rodada)

> Relatórios de evidência por área em andamento (4 agentes de exploração read-only). Linhas serão consolidadas abaixo à medida que os relatórios retornarem.

#### §17 Vínculos profissionais / credenciamento / elegibilidade
Varredura completa: `r8a0-varredura-vinculos-unidades.md` (anexo da rodada).

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Vínculo profissional×tenant×unidade (status, validade, responsável) | materializado em `medicos` (`tenant_id`/`cliente_id`, `reg_status`, `bloqueado` v2341); verificação de idoneidade no aceite `Data.cs:1488-1491` ("Seu vínculo ou elegibilidade profissional mudou."); sem entidade vínculo, enum de status, validade nem `responsavel_verificacao` | parcial | `bloqueado` é lido em regras mas não tem origem administrativa no produto (só inativação de cadastro escreve estado) | fatia R8-vínculos | novo teste |
| Especialidades na operação | `medico_especialidades` + índice único parcial (v2341); `VerificarEspecialidadeCompatívelAsync` (motivo `ESPECIALIDADE`, `BusinessRulesServices.cs:187-192`); score 80×20 em recomendações | parcial | **sem caminho de escrita produtivo** (única INSERT é em teste; tabela nasce vazia em banco real) | poplar/migrar + UI | contrato + comportamento |
| Suspensão bloqueia novos compromissos; futura sinalizada | novos: todos os pontos de decisão (motivo `MEDICO_INATIVO`; 409 `CriarAgendamento_MedicoBloqueado_409`); futura: **nenhuma** varredura/sinalização de escalas futuras ao suspender (busca exaustiva) | parcial | suspensão sem avisar compromissos futuros | fatia §17 | novo teste |
| Documentos com vencimento (CRM etc.) | `crm`/`uf_crm` como campos de cadastro; `VerificarDocumentacaoAsync` retorna sempre `null` → motivo fixo não-bloqueante `DOCUMENTACAO_NAO_VERIFICADA` (`BusinessRulesServices.cs:207,220`) | ausente | sem documento/vencimento/alerta/bloqueio | fatia ou DEC | — |
| Histórico de aprovação/suspensão de vínculo | `historico_escala` (v2342: autor/data/justificativa) + auditoria genérica (`ACEITAR_CONVITE`, inativação médico); nada específico ao ciclo de vida do vínculo | parcial | trilha cobre escalas/inativação, não credenciamento | junto com a fatia | — |
| Revalidação de elegibilidade no momento da ação | `MedicoElegibilidadeService.VerificarElegibilidadeParaPlantaoAsync` (`BusinessRulesServices.cs:153-172`) em aceite/confirmação/substituição/aceite-convite com `pg_advisory_xact_lock` + `FOR UPDATE`; 15 testes R7A1 | pronto | convite **emitido** não roda elegibilidade completa (revalidação ocorre no aceite — documentado em c7a1 §7) | aceitável | suíte |
| Termo "credenciamento" | domínio profissional: **ausente** (0 tabelas/endpoints/colunas; design v2.11.0 registra como pendente); sentido fiscal (credenciais SEFAZ/Opmenexo/INPART) tratado pelo mecanismo honesto de bloqueio | ausente (domínio) | painel administrativo de credenciamento nunca implementado | fatia R8-vínculos (mesma) | — |

#### §18 Operação multi-unidade e recursos compartilhados
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Usuário com acesso a 1..N unidades no tenant | unidades = `hospitais` + `clinica_unidades/salas/setores_atendimento` (escrita real R5-E13); enforcement server-side = **guarda de tenant** sobre a entidade (`TenantGuardService.PodeAcessarHospitalAsync`, `TenantServices.cs:95-98`); **não há** mapeamento usuário×unidade (busca exaustiva) nem claim de unidade | parcial | restrição granular por pessoa ausente — usuário opera em todas as unidades do tenant | decisão (necessidade real?) | novo teste se mapear |
| Contexto de unidade visível nas telas operacionais | propagado por parâmetro de rota/query filtrado server-side (`UnitPortalServices.cs:20-50`, `CentralEscalaController.cs:53-63`, `ConferenciaExecucaoController.cs:14-36`, `ConsultasWorkspaceController.cs:15`) | pronto (escopo atual) | claims de sessão não carregam unidade | junto com a decisão acima | — |
| Recurso compartilhado sem reservas incompatíveis (sala/guichê/setor) | tabelas prontas (`painel_chamada_fila(setor,sala,guiche)`, `agendamentos.sala_id`); **R8-B3: política de conflito de sala canônica** em `ConflitoHorarioService.ListarFontesConflitoSalaAsync`/`ExisteConflitoSalaAsync`, aplicada na criação e no reagendamento de agenda clínica (incluindo mudança apenas de sala) | corrigido (regra de sala em agendamentos) — restante da linha = dívida §5 doc r8b3 | sobreposição simultânea na mesma sala era possível; agora bloqueada nos agendamentos | fatia entregue: `r8b3-conflicto-sala-agendamentos.md` (dívidas: filas de senha, constraint excludente de banco, validação de referência de sala) | 8 testes `R8B3AgendamentoConflitoSalaTests` + regressão R7-A1 em gate (full ×3 = 1290) |
| Transferência de atendimento/material registra origem/destino/responsável | substituição de escala preserva compromisso + histórico (R4/R7-A1); transferência de material = módulo estoque (movimentos com origem) | pronto (escopos cobertos) | transferência de **atendimento** entre sala/profissional não auditada | confirmar na varredura clínica | — |
#### §19 Aprovações e separação de responsabilidades
Varredura completa: `r8a0-varredura-aprovacoes-precos-cancelamento.md`.

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Mecanismo de aprovação (compras, ajustes, estornos, fechamento, contratuais) | padrão repetido por domínio (estado + `aprovado_por/em` + idempotency key): compras `ComprasRepository.cs:93-108`, inventário `InventarioRepository.cs:117-162`, estornos `CotacoesRepository.cs:1376+`/`Data.cs:1989-2007`, fechamento `FechamentoOperacionalService.cs:125`, SaaS `SolicitacoesPlanosController.cs:28-45` | parcial | sem motor genérico único (padrão consistente; sem reuso cruzado) | decisão: manter por domínio vs. consolidar (evitar engine prematura — §19 do prompt) | suíte por fluxo |
| Autoaprovação permitida/impedida conforme configuração | autoaprovação financeira por construção (`processado_automaticamente=true`, :171); regra declarada `comercial_regras_desconto(exige_aprovacao)` **não consumada** (só contract test); bloqueio criador≠corretor só em correções de presença | parcial | sem switch global nem guarda criador≠aprovador em compras/inventário/fechamento | fatia §19 | novo teste |
| Alterar conteúdo após aprovação invalida a autorização | plantões bloqueados pós-confirmações (`Data.cs:1154`); cotação aceita exige estorno (:834-836); pagamento exige estorno (:1979,1999); compra sem edição de itens (congelada) | pronto (por domínio) | sem revalidação uniforme por hash | — | B7_T16 e afins |
| Aprovação registra versão analisada | snapshots: `snapshot_proposta jsonb NOT NULL` (cotações), `orcamento_revisoes(revisao,snapshot_json)`, `triagem_snapshot` (trigger), eventos ADM360 c/ SHA-256 imutáveis (`Adm360EventService.cs:12`) | pronto | compras/inventário: `versao` sem hash de conteúdo | — | `Administrativo360EventosImutabilidadeTests` |
| Delegação com período e escopo | ausente (buscas exaustivas); separação via policies/roles (`Adm360.InventarioAprovar` etc.) | ausente | — | DEC (necessidade real?) | — |
| Negativa exige justificativa | coberto: módulos/planos (`:97`/`:131`), fechamento (motivo catalogado), estornos, inspeção (constraint v2191:13), escalas/assinaturas (Web força motivo) | pronto | substituição de plantão: justificativa opcional | endurecer na próxima fatia de plantões | suíte |
| Repetir aprovação não repete efeitos | `B7_T10_AprovarDuasVezes…SemDuplicar` + unique `ux_adm360_resposta_cotacao_nao_resolvida`; inventário/compras por Idempotency-Key; SaaS `B2_AprovarUpgrade…Idempotente` | pronto | — | — | suíte (linha de aceite §15) |

#### §20 Preços, contratos e vigência
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Vigência em tabelas de preço | `convenio_contratos(vigencia_inicio/fim)` + `contratos_operacionais` + `regras_preco_contrato` + repasses + AI pricing versionado; **mas** convênio-tabela de procedimentos, preços de produtos e `plantoes.valor` **sem vigência** | parcial | cobertura desigual | DEC por domínio (o que precisa vigência) | novo teste se migrar |
| Histórico de versões de regras de preço | `orcamento_revisoes` + `ai_precos_modelos(versao_de/ate)` + `v2327_historico_reg_status`; sem histórico para `regras_preco_contrato`/`convenio_tabelas_procedimentos`/`v115_regras_faturamento` | parcial | regras mudam in-place via reg_status | junto com o item acima | — |
| Critério explícito de escolha da regra | contratos operacionais: `current_date between … and status='ativo'` (`UnitPortalServices.cs:33`); módulos: predicados canônicos v2339/40; fechamento: regra nomeada `VALOR_BASE_12H`; **`regras_preco_contrato` criada/indexada mas nenhum cálculo a consulta** | parcial | tabela de regras órfã | DEC: implementar engine ou remover da promessa | — |
| Valor acordado preservado na operação | pagamentos plantão snapshot completos (`valor_previsto/apurado/aprovado/hora/horas_referencia/parametros jsonb` + `historico_pagamento`); faturas `valor_base_snapshot`; compras congeladas; consultas `triagem_snapshot` | pronto | escalas/convites referenciam `plantoes.valor` vivo (cobrança snapshotada em pagamentos) | aceitável (documentar) | suíte |
| Sem ambiguidade entre regras equivalentes sobrepostas | sobreposição de contratos trabalhistas checada no serviço (`Administrativo360Service.cs:224-225`, testado); `ck_*_vigencia` básicos; sem exclusion constraint no banco para regras de preço | parcial | ambiguidade possível em `regras_preco_contrato`/`convenio_contratos` | junto com DEC de vigência | novo teste |
| Cálculos demonstram componentes (memória de cálculo) | UI "Memória de cálculo" (regra/base/horas/vigência/arredondamento — `Fechamentos.cshtml:27`) + `parametros_apuracao jsonb` persistido + comissões c/ `base_calculo` em relatórios | pronto | fora do ciclo fechamento: subtotais sem memória estruturada | R7-D (exibição) | — |

#### §21 Cancelamento com análise de efeitos
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Consequências apresentadas antes de cancelar | server-side **bloqueia** com 409 fundamentado (plantão c/ escalas confirmadas/pagamento — `Data.cs:1235-1243`; venda c/ títulos baixados — `VendaRepository.cs:376`; pagamento pago exige estorno); UI confirma genérico c/ justificativa; painel de consequências só em substituição | parcial | "apresentar vínculos antes" (estilo dry-run) não existe | fatia §21 (endpoint de análise de efeitos p/ compra+atendimento prioritários) | novo teste |
| Sem exclusão física de histórico protegido | `delete from` zero no runtime (só fixtures de teste); contrato proíbe hard delete de escalas (`Fase4OperationalAutomationContractTests.cs:84`); inativação `reg_status='I'` padronizada + histórico (v2327) | pronto | — | — | suíte |
| Compensação ao cancelar | estornos completos (pagamento/contas a pagar/receber/cotação) + cascata transacional em plantão; **R8-B4: cancelamento de pedido de compra com compensação** — `ComprasRepository.CancelarAsync` + `POST compras/{id}/cancelar`: reverte estoque por grupo, zera títulos, evento no documento fiscal, gates fundamentados (consumo/reserva/título pago → 409), trilha v2347 | corrigido (compra recebida) — dívidas §5 doc r8b4 | pedido RECEBIDO não tinha como ser desfeito; estoque e título ficavam pendurados sem operação de reversão | fatia entregue: `r8b4-cancelamento-compra-recebida.md` | 8 testes `R8B4CancelamentoCompraRecebidaTests` + i18n 244 (full ×3 = 1298) |
| Revalidação server-side + falha parcial consistente | `FOR UPDATE` + estado esperado em todos os pontos críticos; retries SERIALIZABLE (40001/40P01); `AprovacaoDeModulo_RevalidaDependenciasAtivasNaTransacao` | pronto | consistência inter-módulo depende de ação complementar manual (estorno antes de cancelar) | aceitável (documentado) | suíte |
| Exceção administrativa c/ justificativa + auditoria | justificativa obrigatória em todos os cancels + auditoria em todo status change (`audit.LogAsync`, histórico, eventos imutáveis c/ hash); sem override/force dedicado | parcial | exceção = estorno/ajuste manual (funciona, sem trilha própria) | opcional na fatia §21 | — |

#### §22 Convênios: faturamento e glosas
Varredura completa: `r8a0-varredura-financas-pacientes-imports-falhas-notificacoes.md` (área A).

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Lote→envio→retorno→recebimento→glosa→recurso | lotes v116 `ABERTO→FECHADO→ENVIADO_DEMO→RETORNO_DEMO` (`V116ConsolidacaoServices.cs:187`); conta por regra `V115FaturamentoRegraService.GerarContaAsync`; boleto demo honesto; glosa `ABERTA→RESOLVIDA` c/ motivo/valor≤elegível/prazo; recurso Saude360 exige justificativa (`Saude360ClinicalService.cs:808`) | parcial | "envio" é status interno — sem integração externa TISS nem protocolo de retorno | sandbox quando houver credenciais; até lá, estado honesto | contract tests V115 (estáticos) |
| Itens faturáveis + sem duplicidade | `v114_itens_faturaveis` + outbox; 409 + unique parcial `ux_v195_conta_origem_ativa` | parcial | sem teste comportamental da constraint; sem módulo de pendências documentais | novo teste da constraint | novo teste |
| Recebimento parcial mantém saldo + vínculo | `ReceberAsync`: `FOR UPDATE`, 422 excedente, `v115_recebimentos` c/ vínculo, status PARCIAL/PAGA, histórico saldo anterior/novo, estorno c/ origem | pronto (escopo conta clínica) | granularidade ≠ item de lote de convênio | fatia §22 se convênio avançar | suíte |
| Glosa parcial + recurso rastreável → reconciliável | abatida no saldo (`glosa_reconhecida`); prazo gera prioridade ALTA <2 dias; 2 tabelas (`v115_regras_glosa`, `convenio_glosas` v2315) | parcial | documentos anexos ao recurso ausentes; glosa por conta/título, não por item de lote | fatia §22 | teste: lote parcial+glosado reconciliável (§30) |

#### §23 Pacientes: identificação e duplicidades
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Busca por combinações de dados | único termo livre `ilike nome|cpf|telefone|email` (`Saude360ClinicalService.cs:635`) | parcial | sem busca combinada nome+CPF+nascimento | evolução simples | novo teste |
| Duplicidade: CPF exato bloqueia; aviso de similaridade | contagem por CPF normalizado (`:153/:215`) + índice único `ux_pacientes_cliente_cpf_informado`; fuzzy/trigram: ausente (busca exaustiva) | parcial | sem aviso de possível duplicidade | fatia §23 (trigram ou similar) | novo teste |
| Correção cadastral com histórico | `paciente_historico(acao,detalhes jsonb,usuario_id)` inserido em toda mudança (`:752`) | pronto | — | — | suíte |
| Mesclagem controlada c/ impacto + sem cross-tenant | **ausente** (busca `merge/mescla/merged_into` repo-wide → 0) | ausente | operação inteira | fatia §23 | novo teste (§30) |
| Nome social × civil com uso definido | coluna existe e persiste (`base_clinica.sql:10`, `:682`); regra de exibição ausente | parcial | uso em etiquetas/documentos | R7-D (textos/design) | walkthrough |

#### §24 Central de importações
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Importadores reais | XML NF-e/NFS-e Adm360 (`Adm360DocumentosXmlController` + repository c/ quarentena) + CID CSV/URL (`cid_importacoes`); sem planilha p/ clientes/pacientes | pronto (escopo atual) | central unificada não existe | padronizar fluxo (abaixo) | A3/B1/WsA3 tests |
| Fluxo selecionar→validar→prévia→confirmar→processar→resultado | XML: selecionar→validar (2MiB/quarentena)→processar→resultado consultável; **prévia e confirmar ausentes** (grava direto) | parcial | etapas do prompt faltando | fatia §24 | novo teste |
| Erros por item + sucesso parcial | resultado por documento c/ motivo + resumo agregado (`DocumentosXmlRepository.cs:290-325`); CID só totais | pronto (XML)/parcial (CID) | — | — | G2/G3/G5 |
| Repetição não duplica | `UNIQUE(tenant_id,chave_acesso)` + dedup intra-arquivo; testes `G2…Idempotente`, `G3…`, `G5…` | pronto | CID upsert sem teste nomeado | — | suíte (linha §15) |
| Processamento longo c/ progresso | síncrono (teto 2MiB); sem job/progresso | parcial | arquivos maiores | assíncrono se necessário | — |

#### §25 Recuperação de falhas e reprocessamento
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Estado de processamento persistido | máquina de estados de cotações (`ENVIANDO/RESULTADO_DESCONHECIDO/INTERRUPTA…`, v2304) + outboxes (v113 manual, v116 operacional) | pronto (Adm360) | outbox clínico sem worker (ações manuais) | DEC | B5 tests |
| Tentativas + última falha + correlação | linha por tentativa (canal/mensagem/protocolo/times), `UNIQUE(tenant,resposta,tentativa)`, correlação por resposta_id / payload_ref | pronto | erro de outbox v113 preenchido manualmente | — | B5 |
| Reprocessamento autorizado | `transmitir?confirmarRetransmissaoDesconhecido=` (policy), outbox retry/process/error, jobs reprocessar, notificações reprocessar | pronto | — | — | B5 |
| Detecção de processamento interrompido | recovery no boot + hosted service 300s (`Adm360TransmissaoRecoveryHostedService`) | pronto | escopo Adm360 | — | B5 (`EnvioInterrompido_…`) |
| Timeout externo → resultado desconhecido consultado antes de retry | `RESULTADO_DESCONHECIDO` + confirmação explícita exigida; já ACEITA bloqueia citando protocolo (testes B5 :353/:497) | pronto | fora de Adm360: classificação manual | — | suíte |
| Restart não perde estado | reconciliação de boot ("nunca apagadas nem inventadas") + teste; **outbox v113/v116 retém PENDING sem retomada automática** | pronto (Adm360)/parcial | retomada automática ausente nos outboxes clínicos | worker ou DEC | novo teste |

#### §26 Notificações e comunicação contextual
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Canais | in-app (legacy `plantaopro.notificacoes` + Operation360 `notifications/recipients` + realtime); **email/whatsapp sem enviador** (preferências gravadas, nunca lidas) | parcial | canais externos inexistentes | DEC de escopo | — |
| Estados criada/enviada/entregue/lida | criada/lida/resolvida/arquivada persistidas (`notification_read_states`, ações); enviada/entregue ausentes (coerente) | parcial | — | junto c/ canais | — |
| Dedupe em retries | Operation360 unique + `on conflict do nothing` (`NotificationService.cs:97`); legado **com dedupe parcial** via `ux_notificacao_legado_origem` (v2346) | **corrigido** (R8-B2) | legado duplicava em repetição; agora `on conflict do nothing` p/ origens declaradas nos 21 call sites | doc `r8b2-notificacoes-legado-dedupe.md` | teste `R8B2NotificacoesLegadoDedupeTests` 5/5 + suíte 1282 |
| Preferências não anulam avisos obrigatórios | preferências por usuário; `in_app` fixo true; sem classe de aviso obrigatório | parcial | política de obrigatórios ausente | fatia se canais avançarem | — |
| Templates idioma/tenant/identidade visual | ausente (strings PT-BR hardcoded; tabelas `tenant_parametros_notificacoes`/`notificacoes_avancadas` mortas) | ausente | motor de template | futuro (após i18n) | — |
| Alteração de escala gera aviso rastreável | notificação **na mesma transação** em todas as mudanças de escala + `IAuditService` + auditoria de leitura | pronto | in-app apenas | — | suíte (linha §30) |

#### §27 Indicadores com definição e rastreabilidade
Varredura completa: `r8a0-varredura-indicadores-design-redundancias.md`.

| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Definição por indicador (fórmula/fonte/período/fuso/filtros) | Relatórios Operacionais: **único com `Definition` na UI** (`OperationalReportService.cs:79-84`); BI Executivo c/ janela+fuso validado (`BiServices.cs:36-49`); Meu Dia (UTC fixo)/CommandCenter/Premium/dashboard API sem definição | parcial | maioria dos painéis sem definição exposta | fatia §27: estender padrão Operacional | contrato de definição |
| Zero ≠ informação indisponível | distinguido em Operacional/Home Dashboard/Pagamentos/_EmptyState; **Bi/Index e MeuDia exibem 0 em falha de API** (`??0`/`toFixed` sem catch) | parcial | falha mascarada como zero | fix de estado de erro Bi/MeuDia | novo teste |
| Denominador vazio tratado | guards `nullif`/`case when` em todas as taxas localizadas (5+) | pronto | pattern ad-hoc, sem helper | aceitável (nota) | — |
| Abrir registros que compõem o total | Operacional ("Ver registros"/"Abrir origem"), CommandCenter ("Ver detalhe"), MeuDia (ação primária); cards Bi/Premium sem link | parcial | KPIs sem drill-down | estender padrão | contrato |
| Export c/ mesmos filtros + permissões | `CsvAsync` delega a `GetAsync` (mesmo SQL/filtros/guards, `OperationalReportService.cs:33-50`); SaaS export c/ guard + auditoria | pronto | export CSV ausente em relatórios financeiros Adm360 (Web exporta à parte) | alinhar se necessário | suíte |
| **Bug**: label "Plantões do mês" sem filtro de mês | `Fase6BiIntegracoesServices.cs:53,56` | **corrigido** (R8-B1) | indicador mentiroso + 2 bloqueadores na mesma statement (tabela inexistente no `total_receber` → 500; leitura dinâmica zerando KPIs) | filtro de mês corrente + tabela canônica + tupla tipada; doc `r8b1-fase6bi-plantoes-do-mes.md` | teste `R8B1…` 1/1 + suíte 1277 |

#### §28 Design de ações complexas
| Recurso | Evidência (HEAD) | Estado | Problema | Ação | Validação |
|---|---|---|---|---|---|
| Wizards c/ passos visíveis | Onboarding NovoCliente: stepper 5 passos, **único POST** (etapas visuais, não por passo); importações form único | parcial | passos reais persistidos | oportunidade no R7-OB | novo teste |
| Resumo antes de confirmar efeito amplo | `_ConfirmModal` + mensagens descritivas (Escalas, Financeiro, Assinaturas c/ motivo 500 chars, Inventarios, PedidosCompra, revogação em Usuarios); ausente onde a ação não existe (cancelar compra, mesclar paciente) | pronto (padrão) | casos novos dependem das fatias §21/§23 | — | contrato |
| Aviso de alterações não salvas | `form-experience.js` (dirty + beforeunload) em ≥12 forms marcados | pronto | só forms marcados | expandir cobertura | — |
| Rascunho clínico c/ indicador de salvamento | rascunho em plantões/pedidos/orçamentos/vales; **consultas clínicas sem rascunho** (AGUARDANDO→FINALIZADA) | parcial | §9 pede explicitamente | fatia clínica | novo teste |
| Recuperação após erro preservando preenchimento | `return View(model)` em 40+ controllers; TempData preserva justificativas | pronto | forms AJAX não verificados em 5xx | spot e2e | — |
| Diferenciação visualizar/editar/aprovar/executar | consistente em domínios novos; domínios antigos com botões genéricos cinza | parcial | regra verbo→estilo não no design system | R7-D | checklist |
| Botão desabilitado explica o porquê (e servidor autoriza de qualquer forma) | motivos específicos em Convites/PlantoesDisponiveis/_ModuleLockedNotice; genéricos no resto; autorização server-side independente confirmada | parcial | tooltips genéricos | R7-D | checklist |
| Operações em lote c/ seleção/contagem/efeito/resultado | Comissões Pendentes: select-all + contagem + efeito + bulk transacional idempotente (`ContasPagarRepository.cs:686-777`); resultado item-a-item não verificado | parcial | relatório de resultado por item | evoluir | novo teste |
| Tabelas c/ texto longo/registos muitos | paginação (limites 100) + filtros preservados via hidden inputs; truncation só horizontal (`table-responsive`), sem por célula | parcial | textos longos quebram colunas | R7-D | checklist |

#### §29 Revisão de redundâncias (mapa — sem conclusão, ordem §29 do prompt)
Top 10 candidatos mapeados com consumidores (varredura completa no anexo):
1. **5 calculadoras de dashboard/KPI** (`BiService`, `DashboardPremiumService`, `Fase6…DashboardAsync`, `DashboardService`, v114/operacoes) — inclui o bug "Plantões do mês" (§27).
2. **`cid_tabela` criada divergente em 6 arquivos** (seeds/migrations) — risco de drift de schema.
3. **Mirror de DTOs Api↔Web ×4** (ManagerCommandCenter, Admin360Resumo, ProductivityPage, DashboardPremium).
4. Placeholder "Sem dados no período" duplicado em 5 views + `BiController` 8 ações/4 views (views-fantasma).
5. Menu `_AppSidebar` com **Usuarios.Index ×2 e Perfis.Index ×2** (:75/:76 vs :89/:90) — duplicação visível para super-admin.
6. Comissão calculada/apropriada em **4 locais** (baixa, geração AP, relatórios, UI) — validar canonicidade da fórmula.
7. `parceiros` (B2B jsonb) × `adm360_parceiros` (master c/ flags) — propósitos distintos, sobreposição semântica.
8. `consultas.especialidade` text + FK `especialidade_id` coexistindo.
9. `MeuDiaService` facade sobre `ProductivityActionService` (compatibilidade intencional).
10. Cards KPI manuais × partials `_KpiCard`/`_CoverageCard` subutilizados.
**Estado: mapa pronto.** Consolidação segue o rito do §29 (comparar consumidores → canônico → migrar → regressões); nenhuma deleção nesta rodada.

## 3. GATE (§3 do prompt) — situação local

| Verificação | Status |
|---|---|
| Build dos projetos afetados | ✅ build10: 0 erros (11/10 08:11) |
| Login e sessão funcionais | ✅ e2e web-bff (suíte) |
| Rotas sem duplicidade | ✅ investigado na C7-A0; monitorar novas rotas |
| Permissão/negação por perfil | ✅ guard + motivos + testes dedicados |
| Isolamento tenant | ✅ múltiplos e2e |
| Migrations em ambiente descartável | ✅ v2339→v2344 em `plantaopro_test` |
| Persistência comprovada | ✅ suíte de integração (PostgreSQL real) |

**GATE satisfatório localmente.** Homologação viva segue etapa separada (roteiro `r6a1b` §7, inclui agora o ciclo convite→aceite e o upgrade v2339→v2344 do banco principal).

## 4. Ordem de cortes proposta (atualizada após esta auditoria)

1. ~~**R7-OB** — passo "Preferências" (idioma) no onboarding~~ — ✅ **entregue nesta rodada** (v2345 + avaliador + 5 testes; doc `r7ob-onboarding-preferencias-idioma.md`; fecha a linha da matriz e o uso na jornada de §12.6). Wizard MNSOFT `NovoCliente` segue fora do escopo desta fatia.
2. **R7-D** — varredura design/textos/i18n por jornada: escopo consolidado em `r7d0-varredura-design-textos-i18n.md` (6 fatias). **F1–F6 entregues nesta rodada** (sidebar/topbar, login/perfil, central de escala, jornada clínica Saúde360, onboarding/implantação e Admin 360 cotações/dashboard/resumo organizacional + convenção de `TempData["Success"/"Error"]` em `Adm360*`; docs `r7df1-navegacao-i18n.md`, `r7df2-login-perfil-i18n.md`, `r7df3-central-escala-i18n.md`, `r7df4-saude360-i18n.md`, `r7df5-onboarding-implantacao-i18n.md`, `r7df6-admin360-cotacoes-dashboard-tempdata-i18n.md`; suíte agora 1276 testes com gate 52; resx com 851 chaves por arquivo). Dívidas R7-D restantes (registradas no doc do F6): `_SuprimentosNav.cshtml` (~45 literais compartilhados), erros de formulário `ComFormError`, demais views sob `Views/Administrativo360/`.
3. Cortes novos derivados dos achados §17–§30 (definidos após consolidação desta matriz; prioridade: itens classificados quebrado, depois ausente-alto-impacto).
4. Externos/DECs: upgrade formal `plantaopro` v2339→v2344 + IIS; homologação viva; credenciais fiscais; semântica de `solicitado`; DECs de R6-A1b.

## 5. Notas

- Ambiente de execução: máquina instável (thrash de RAM; reiniciões de servidor durante builds/testes). Assinatura de falha ambiental (lenta ≥1 min, variável) × falha determinística de contrato (ms, repetida) documentada na rodada 7. Execução v2 da suíte com timeouts ampliados (`VSTEST_CONNECTION_TIMEOUT=600000`, host factory 900 s).
- Commit `2f4533d3` ("novos") criado/empurrado externamente; conteúdo auditado e confirmado (31 arquivos = pacote R7-E1 + docs + 3 contratos atualizados).
- Suíte: 1253 testes após R7-OB (1248 + 5 novos). Leitura final da validação v2 da rodada em §7.

## 6. Correções pontuais aplicadas durante esta auditoria (R8-A1 — commit `cedd209`)

Dois defeitos reais foram confirmados pela execução da suíte v2 (1ª tentativa) e corrigidos no mesmo ciclo:

| # | Defeito | Evidência da falha | Correção (HEAD de trabalho) | Validação pendente |
|---|---|---|---|---|
| 1 | **Corrida do painel de chamada**: a guarda R7-A1 usava o estado lido instantes antes (`status=@esperado`); uma chamada que lia a linha **depois** do commit da mais rápida revalidava a própria transição (CHAMADO→CHAMADO) e virava sucesso duplicado | `Chamar_FilaConcorrente_DoisOperadores_SomenteUmaReservaESemHistoricoDuplicado` [65 ms] em `v2-full1.log` (r1 e r2 = 200; XOR violado); não reproduziu em full2/full3 → assinatura temporal (corrida real, janela estreita) | `Saude360ClinicalService.cs`: ancoragem do **estado-fonte permitido por ação** no predicado do UPDATE — `chamar` ⇒ `status in ('AGUARDANDO','RECHAMADO')`; `rechamar` ⇒ `status='CHAMADO'` (repetível por natureza); `finalizar` ⇒ `status<>'FINALIZADO'`; guarda de estado lido mantida como segunda trava | teste da corrida (agora no gate da fase A) + suíte ×3 |
| 2 | **Bug do teste R7E1 (não do produto)**: `ApiReal_SalvaColunaV2344_…` enviava `POST api/usuarios/me/idioma` sem header Bearer (o token só era anexado no passo 3) → 401 determinístico | `v2-full1.log` :1173-1176 (`Expected: OK / Actual: Unauthorized`, `R7E1I18nFundacaoTests.cs:437`); `PlantaoProApiFactory` não injeta autenticação padrão (confirmado por busca de handlers) | `R7E1I18nFundacaoTests.cs`: anexar `Authorization: Bearer <token do login>` antes do passo 2 | teste `ApiReal_SalvaColunaV2344_E_RefleteNoRefreshENoNovoLogin` verde no banco real |

Observações de escopo:
- A 1ª execução do pipeline v2 morreu antes de rodar por causa de um probe de RAM com label em inglês num `systeminfo` pt-BR (`.Trim()` sobre nulo) — corrigido para probe CIM locale-independent; as 3 fases que "rodaram" nessa tentativa morreram em ~18 s cada (crash precoce de testhost), logo **só as falhas nomeadas acima têm valor de evidência**, e ambas foram tratadas.
- Janela análoga (leitura do estado antes do commit) existe em `iniciar` de consultas/triagens; efeito quase-idempotente (dupla linha de histórico). Registrada como backlog DEC, fora deste corte.

### Leitura final da validação v2 (rebuild build11 pós-R8-A1)

| Fase | Resultado | Duração | Log |
|---|---|---|---|
| Gate: 3 contratos + R7E1 (29) + R7A1 (15) | **47/47 aprovados** | 10 s | `v2-phaseA-contracts-r7e1.log` |
| Suíte completa ×1 | **1248/1248** | 15 s | `v2-full1.log` |
| Suíte completa ×2 | **1248/1248** | 16 s | `v2-full2.log` |
| Suíte completa ×3 | 1247/1248 | 16 s | `v2-full3.log` |

- Falha única do ×3: `SaasGovernancaB6Rodada4Tests.G3_Trial_ValidoOpera_E_VencidoBloqueiaComRegistro` — `Npgsql 40P01` (impasse) em `PurgarAsync` dentro de `Dispose` (`SaasGovernancaB6Rodada4Tests.cs:481`, invocado em :78); assertions do teste passaram. Assinatura = flake de cleanup sob concorrência de coleções no banco compartilhado, não regressão de produto. **Reexecução isolada da classe: 8/8 em 1 s** (`rerun-b6.log`).
- Com isso, o status **TESTADO** do R7-E1 fica selado com evidência executada (incluindo persistência real da coluna v2344 no PostgreSQL e o teste de corrida do painel agora no gate permanente).
- [R7-D-F3] Falha única do full2 do ciclo 10:42 UTC-3: `Administrativo360DocumentosXmlB1Tests.Divergente_MesmaChave_OutroConteudo_FalhaComMensagemDeErro` (`:356`, `r2.Falhas` esperado 1 / obtido 0) — full1 e full3 passaram com o mesmo binário; **classe isolada 6/6 em 1 s** (`rerun-b1.log`). Terceira assinatura distinta de flake sob concorrência de coleções no banco compartilhado (as duas anteriores: 40P01 em `PurgarAsync` cleanup); não é regressão do corte i18n (zero contato com o repositório de XML).
- [R7-D-F4] Falha única do full1 do ciclo 11:16 UTC-3: `SaasComercialB4MatrizTests.B5_AtivarAgendados_SoVencidoViraAtivo_ComTrilha_E_Idempotente` — `Npgsql 40P01` em `PurgarAsync`/`Dispose` (`SaasComercialB4MatrizTests.cs:417/65`), **assinatura ① já catalogada acima**; full2/full3 verdes com o mesmo binário (1268/1268); **classe isolada 12/12 em 1 s** (`rerun-b4.log`).

## 7. Fechamento da rodada até aqui — corte R7-OB entregue

A primeira fatia da ordem de cortes (§4.1) foi implementada e validada nesta rodada:

| Peça | Estado | Evidência |
|---|---|---|
| Migração `v2345` (etapa de catálogo `ONB_PREFERENCIAS`, opcional, ordem 50) | aplicada em `plantaopro_test` via `PlantaoPro.Tools.Database upgrade` (checksum conferido pelo tool) | `build12.log` / `upg14.log`: "upgrade: 2026_10_v2345… aplicada." |
| Avaliador `IDIOMA_PREFERENCIA_DEFINIDA` em `OnboardingJornadaService` | mesmо predicado do login (R7-E1): `tenants.metadata->>'idiomaPadrao'` OU usuário ativo com `idioma_preferido` | casos O2/O3 dos testes |
| `R7OBOnboardingPreferenciasIdiomaTests` (5 fatos) | **5/5 isolados**; inclusos no gate permanente | `run-r7ob.log`, `build13.log` |
| Validação v2 pós-R7-OB (09:17 UTC-3) | gate **52/52** · full1 **1252/1253** · full2 **1253/1253** · full3 **1253/1253** — falha única do full1 = 40P01 em cleanup (`PurgarAsync`/`Dispose` do `SaasComercialB4MatrizTests`, segunda ocorrência da assinatura já catalogada; classe 12/12 isolada em 1 s) | `suite-final2-summary.txt`, `v2-full*.log`, `rerun-b4.log` |
| Web | zero mudança (checklist genérico renderiza a etapa após sincronizar) | mesmo contrato das C7 |

Observações:
- `IDENTITY_SCHEMA_INCOMPLETE MISSING_ADMIN_ROLE_LINK count=1` no upgrade é condição
  pré-existente do banco de teste (o upgrade termina 0); não foi introduzida por v2345.
- Lição registrada no doc da fatia: o tool `PlantaoPro.Tools.Database` não aceita conexão
  em formato URL; usar chaves (`Host=...;Database=...`).
