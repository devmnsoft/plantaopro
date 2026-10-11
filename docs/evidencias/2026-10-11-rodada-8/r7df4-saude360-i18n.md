# R7-D-F4 — Saúde 360 (jornada clínica): estado vazio específico por módulo + stub de Consultas + etapas em recursos localizados

Data: 2026-10-11 · Base: `7ab0989` (R7-D-F3). Fatia 4 do corte R7-D (escopo em
`r7d0-varredura-design-textos-i18n.md`, seção 6, linha F4).

## Problema (achado R7-D0, jornada 1)

A família de ~49 telas clínicas renderiza 3 parciais compartilhados; todos seguiam
com pt-BR hardcoded:

- `Views/Saude360/Modulo.cshtml` — **renderizador da tela-padrão dos 49 módulos**
  (cada `Views/{Módulo}/{Ação}.cshtml` é um wrapper de 5 linhas que chama este
  parcial). Continha: painel de ajuda (9 textos), eyebrow, navegação da jornada
  (aria + 8 passos + 7 contagens), bloco "Próxima ação", bloco de contexto
  longitudinal do paciente, **estado vazio genérico "Nenhum registro encontrado"
  em 2 pontos (fora e dentro da tabela) sem nome do módulo e sem CTA real**,
  KPI strip (plano/registros/acesso), tabela de registros (5 headers) e banner LGPD;
- `Views/Saude360/Formulario.cshtml` — **renderizador de todos os formulários
  Create/Edit da família**: sublinhado do cabeçalho, botão Voltar, hint
  visually-hidden, 5 seções, ~25 labels, 5 lookups (labels + placeholders),
  select de classificação de risco (+ ajuda), 8 sinais vitais, seção de cobrança
  (incl. "Marcar como principal") e rodapé (Cancelar/Salvar/Finalizar triagem/
  Encaminhar para consulta, com títulos disabled);
- `Views/Consultas/Index.cshtml` — **stub estático de 18 linhas sem `@model`**:
  ignorava o `Saude360PageViewModel` inteiro (registros, ações, plano, permissão)
  e mostrava só 4 badges decorativos — a única tela da família que não usava o
  renderizador padrão;
- `Views/Shared/_PageHelp.cshtml` (3 consumidores: Modulo, Formulario, Manual) e
  `Views/Shared/_LookupSelect.cshtml` — rótulos próprios hardcoded
  ("Você está aqui", "Quando usar:", "Próximos passos", "Dicas rápidas", "Manual";
  "Registro selecionado", "Não encontrou? Veja como cadastrar novo.").

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `UiTexts.resx` (+ `.en/.es/.fr`) | **+126 chaves** (`Clinical.*`) em paridade total nos 4 arquivos (**478 chaves por arquivo**, 0 duplicatas, 0 missing/extra conferido por script de paridade) | paridade R7-E1 + contrato novo |
| `Saude360/Modulo.cshtml` | tudo via recursos; **estado vazio des-genericizado**: título `string.Format(InvariantCulture, UiText.L("Clinical.EmptyTitle"), vm.Modulo)` ("Nenhum registro de {módulo}") + CTA real = `Acoes[0]` da ViewModel (`EmptyStateViewModel(…, ButtonDisabled=false)` quando há ação, desabilitado quando não); jornada/ajuda/KPIs/tabela/LGPD localizadas; **guardas, rotas `asp-*`, data-attrs e condições intactos** | contrato `R7DF4…Saude360Modulo_…` |
| `Saude360/Formulario.cshtml` | seções/labels/placeholders/vitais/botões via recursos; valores de máquina do select de risco (`EMERGENCIA`…) e guardas (`data-triage-rules`, `required=…`, disabled) íntegros | contrato `R7DF4…FormularioEParticulares…` |
| `Consultas/Index.cshtml` | **stub → wrapper padrão da família** (mesmas 5 linhas de `Triagem/Index.cshtml`): agora herda título/descrição/CTAs reais (`Nova`→Create, `Atendimento`, …), estado vazio específico, KPIs e tabela — a entrada ganha estado vazio + CTA real | contrato `R7DF4…ConsultasIndex_…` |
| `Shared/_PageHelp.cshtml` + `Shared/_LookupSelect.cshtml` | 6 + 2 rótulos fixos via recursos | idem |
| `V184BusinessActionsContractTests.cs` (gate) | contrato antigo ancorava o literal "Salve a triagem para obter um identificador real" no Formulario → migrado para a chave `Clinical.TriageFinishDisabledTitle` (padrão da F2: contrato literal→chave); demais asserções (endpoint `{id:guid}/finalizar-tipado`, `FinalizarTriagemAsync`, `asp-action="Finalizar"`) intactas | gate passa com a chave |
| Testes `R7DF4Saude360I18nContratoTests.cs` (novo) | 4 fatos de contrato de fonte: chaves presentes + literais ausentes nos 5 views/parciais, valores de máquina íntegros, Consultas wrapper, amostra de chaves nos 4 resx | **4/4 aprovados** (no alvo 76/76) |

## Decisões registradas

1. **Reuso deliberado de chaves existentes** (mesmo padrão F3): `Nav.SectionClinical`
   ("Saúde 360", já traduzido nas 4 línguas pela F1) compõe o eyebrow/kicker junto do
   nome da marca `PlantãoPro`; `Clinical.JourneyTriage/Patient/Appointment/Consult`
   reutilizados onde a mesma palavra é rótulo (bloco do paciente, labels de lookup);
   `Shift.Back/Start/End/Value/ThSpecialty` (F3) e `Profile.Phone` (F2) para
   "Voltar"/"Início"/"Fim"/"Valor"/"Especialidade"/"Telefone". Registro único de cada
   frase no catálogo.
2. **Chaves com placeholder (`Clinical.EmptyTitle`, `Clinical.RecordsTableAria`) usam
   `string.Format(InvariantCulture, …)`** — montagem independente de cultura (padrão
   `Auth.ErrorAuthFailedWithRef`).
3. **Estado vazio duplicado (parcial fora + linha na tabela) foi mantido em ambas as
   posições**, apenas des-genericizado — a remoção da duplicação é mudança de
   comportamento visual fora do escopo desta fatia.
4. **Expansão de escopo registrada** (mesma regra da F3): o anexo R7-D0 apontava
   `Modulo.cshtml:29-100` + Consultas + etapa-jornada (~15 chaves); a leitura revelou
   que `Modulo/Formulario/_PageHelp/_LookupSelect` são os 3+2 renderizadores
   compartilhados da família inteira — deixar Formulario literal deixaria todo
   formulário Create/Edit da jornada em pt-BR. Fatia fechou os 5 arquivos: 126 chaves
   em vez de ~15, mesmo mecânica, zero mudança de comportamento.
5. **Strings que vêm dos controllers** (`vm.Titulo`, `vm.Descricao`, `vm.Permissao` =
   "Permissão por tenant, perfil e plano para {modulo}." montada em
   `Saude360WebControllerBase.PlanoModulo/PermissaoModulo`, `~60` chamadas de
   `ModuloAsync` com pt-BR) e **mensagens dinâmicas da API** (`vm.ErrorMessage`)
   seguem passando direto — dívida do lado controller/API documentada, mesmo padrão
   das F2/F3; keyizar ~60 títulos de módulo é fatia à parte.
6. **Valores de máquina íntegros**: opções `EMERGENCIA/MUITO_URGENTE/URGENTE/POUCO_URGENTE/NAO_URGENTE`
   do select de risco, `data-clinical-mvp-journey`, `data-triage-rules`,
   `data-next-action`, `data-saude360-form` etc. — consumidos por JS/guardas.
7. O terceiro consumidor do `_PageHelp` (`Manual/Manual.cshtml`) passa ainda strings
   próprias pt-BR — saída coberta pela fatia do Manual; os rótulos do parcial agora
   estão localizados para os três.

## Validação

- Paridade de resx pré-build (script local): **478 chaves por arquivo, 0 duplicatas,
  0 missing/extra** entre pt-BR/en/es/fr.
- Build Web (Razor): **0 erros** (`build17.log`, `BUILDWEB17_EXIT:0`); build de Tests
  1ª passada com 3 `CS1003/CS1525` em `R7DF4…Tests.cs:121` (string verbatim com aspas
  simples internas) — corrigido para `""` e verde (`BUILDTESTS18_EXIT:0`,
  `BUILDTESTS19_EXIT:0`).
- Testes alvo (R7DF1 + R7DF2 + R7DF3 + R7DF4 + R7E1 + V2150 + V2156 + V2164 + V2165):
  **76/76 aprovados em 2 s** (`build18.log`) após 1 ajuste de asserção própria
  (`"Carteirinha"` colidia com `asp-for="NumeroCarteirinha"` → literal exato `">Carteirinha<"`).
- Gate da rodada + suíte completa ×3 (11:16 UTC-3, `suite-final2-summary.txt`):
  gate **52/52** · full1 **1268/1268** menos 1 flake · full2 **1268/1268** ·
  full3 **1268/1268**. Falha única do full1:
  `SaasComercialB4MatrizTests.B5_AtivarAgendados_SoVencidoViraAtivo_ComTrilha_E_Idempotente`
  com `Npgsql.PostgresException 40P01: impasse detectado` em `PurgarAsync/Dispose`
  (`SaasComercialB4MatrizTests.cs:417/65`) — **assinatura ① já catalogada** (banco
  compartilhado sob concorrência de coleções); full2/full3 passaram com o MESMO
  binário e **reexecução isolada da classe: 12/12 em 1 s** (`rerun-b4.log`). Fora do
  escopo desta fatia (corte Web/i18n; zero contato com o ciclo comercial) e não
  bloqueia o TESTADO.
- Novos 4 tests no total (1264 → **1268**), todos em `R7DF4Saude360I18nContratoTests`.

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO.
Commit local desta fatia; push diferido por decisão da rodada.
