# Rodada 103 (r103a) — Integrações: WhatsApp opt-in, e-mail transacional e API pública com chaves

Linha 98 do plano (`docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md`): "integrações: WhatsApp (opt-in), e-mail transacional, API pública com keys". A rodada fecha os três pilares num pacote único, sem segredos em appsettings e sem DDL adicional além da migração v2380.

## O que mudou

### Banco (migração v2380, idempotente e transacional)
- `plantaopro.mensagens_saida`: caixa de saída única dos dois canais (`WHATSAPP`/`EMAIL`) com `tipo_evento`, `assunto`, `status` (ENVIADA/FALHOU/BLOQUEADA_SEM_CONSENTIMENTO/SEM_DESTINO), `provider` (default LOCAL) e constraints de domínio.
- `plantaopro.api_chaves_usos`: trilha de uso real de cada chave pública (metodo, rota, status HTTP, IP), escopada por tenant.
- `plantaopro.api_rate_limits`: colunas `api_chave_id`, `limite_minuto`, `limite_dia` + índice.
- `plantaopro.api_chaves`: coluna `ultimo_uso_em`.
- Manifest: 150 entries; v2380 depende de v2379, checksum `d5f01002...e897` (regra LF).

### API
- `Security/ApiKeyAuthenticationHandler` (esquema `ApiChave`): autentica por header `X-Api-Key` contra o hash SHA256 em `api_chaves`; exige chave ATIVA e instituição não suspensa; sem o header devolve NoResult (o JWT segue como padrão); atualiza `ultimo_uso_em` best-effort.
- `EscopoChaveAttribute` (filtro assíncrono): antes do action, aplica 401 (chave ausente), 403 (escopo insuficiente) e 429 (janela 1 min / 1 dia via `api_chaves_usos` + limites dos claims); depois do action registra a trilha best-effort. Aplica-se também à rota `identidade` (escopo vazio: rate limit + trilha sem checagem de escopo).
- `MensageriaService` + `MensageriaController` (`api/mensageria`, audiência clínica SAUDE360 + gate de módulo): opt-in por paciente seguindo `consentimento_lgpd` + `consentimento_lgpd_canal`, envio manual, outbox e resumo; provider default LOCAL; `SEM_DESTINO`/`BLOQUEADA_SEM_CONSENTIMENTO` retornam 200 com status gravado; auditoria MENSAGERIA_OPTIN/MENSAGENS_SAIDA.
- `IntegracoesPublicaService` + `PublicoController` (`api/publico`, exclusivamente ApiChave): `identidade`, `agendamentos/{id}/situacao` (escopo `agendamentos:read`) e `pacientes/{id}` (escopo `pacientes:read`), todas escopadas pela claim `cliente_id` (= clientes.id, semântica do kernel clínico); paciente legado com apenas `nome` cai em fallback via `nullif(nome_completo,'')`.
- `Saude360ClinicalService`: gatilho de check-in — após o audit, best effort, grava uma mensagem por canal com opt-in (`tipo_evento=CHECKIN_REALIZADO`, assunto em EMAIL, projecão tipada); falha de mensageria nunca quebra a ação clínica.
- `B2BLaunchServices.ListarApiKeyLogsAsync`: logs do portal do desenvolvedor agora leem a trilha real em `api_chaves_usos` (100 por chave, escopada por tenant).
- `Program.cs`: registro do esquema `ApiChave` + DI dos dois serviços.

### Web
- `NotificacoesController.Preferencias`: renderiza no servidor o outbox (30) + resumo com degradação graciosa (falha de comunicação → outbox vazio, tela não bloqueia).
- `Preferencias.cshtml`: seção do outbox dos canais externos (data-mensageria-outbox/tabela, data-resumo-sent); a tabela fica dentro do `.table-responsive` (contrato R45).
- i18n: 12 chaves `Integrations.*` com paridade nos 4 resx (master pt-BR), sem jargão fora da KEEP (contrato R73).

### Contratos reconciliados pelos novos objetos
- `mensagens_saida` carrega `paciente_id` → entrou em `TabelasComPacienteId` do serviço de mesclagem (espelho do contrato R8B6 atualizado): ao mesclar pacientes, os vínculos da caixa de saída também são re-apontados para o alvo.

## Decisões e achados empíricos (drill iterado)
1. **Aliases não cotados do Postgres voltam em minúsculas** no result set; projeções tipadas do Dapper são insensíveis a maiúsculas, mas o acesso `dynamic` (DynamicParameters) é exato — o gatilho de check-in usa record tipada (`CheckInPacienteLinha`).
2. `INSERT ... VALUES (...) FROM ...` é inválido (42601) — o insert do gatilho usa `INSERT ... SELECT`.
3. `pacientes.nome_completo` é `not null default ''`: o coalesce simples nunca cai no `nome`; queries públicas usam `nullif(nome_completo,'')`.
4. Máquina de estados do kernel: check-in só de `CONFIRMADO` (AGENDADO → confirmar → CONFIRMADO → CHECKIN_REALIZADO); o drill segue esse fluxo real.
5. Rate limit lido dos claims por pedido (atualização de `limite_minuto` tem efeito imediato no pedido seguinte).

## Evidências
- Drill `r103a_drill_integracoes_whatsapp_email_api_publica.ps1`: **RESULTADO=PASSEI** (migração, plano R103_PLANO_DRILL, módulos em contrato, fixture clínica, opt-in LGPD, envios ENVIADA/SEM_DESTINO/BLOQUEADA_SEM_CONSENTIMENTO, criação de chave pp_live_*, hash SHA256 conferido no banco, 401 chave inexistente, identidade, T1 situacao AGENDADO com nome do paciente, leitura por escopo, 403 sem escopo, 429 janela de minuto com limite restaurado, trilha=3 + logs portal=3 + revogada 401, checkin via API com confirmar prévio, gatilho 2 canais com destinos conferidos, web outbox resumo_enviado=4, portas livres ao final). Dump T1: `r103a_t1_publico_situacao.json`.
- Gate A/B (`r87abuild.ps1 -Tag r103`): base-exit=0, nova-exit=0, 76 warnings únicos em ambos os lados (contagem líquida zero; o único diff textual é o par de CS8600 pré-existente em `Saude360ClinicalService.cs` deslocado pelas linhas do gatilho) — log `r103_gateab_final2.log`.
- Contrato: `R103IntegracoesWhatsappEmailApiPublicaContratoTests` (10 fatos serial: drill, T1, handler, filtro, mensageria, pública, gatilho check-in, B2B logs, web+i18n, migração+manifest).
- Suítes pré/pós-doc: 1852 testes (baseline 1842 + 10 novos), docscan 0 defeitos, EOL CRLF sem BOM.

## Contratos preservados
R24, R8B12, R91, R17/R18, fixtures R99/R100, EvolucaoProdutoVendavelContractTests, paridade 4 resx (1299+12), CobrancaB6Tests C7, R97, SaasInteligenteContractTests e R102 (8 fatos) seguem verdes na suíte integral.

## Pendências futuras (mantidas/registradas)
Grants RELATORIOS.*/TRIAGEM.*/NOTIFICACOES no ADMINISTRADOR_CLIENTE (lacuna de catálogo; drill concede via usuario_permissoes_especiais); ponte caixa in-app × central operacional; avisos pautados não recolhidos ao concluir; notificacoes.tenant_id nulo; ativar módulos em planos reais; WP-A1; ClientPortal descartada + /OperacaoInteligente órfã; conciliar scrpt_completo × manifest; retenção de backups; retroatividade de sincronizar; endurecer wrapper B1; 7 constraints NOT VALID fora do escopo adm360; insert legado do AuditService para plantaopro.auditoria(...); provedores externos reais (SMTP/WhatsApp gateway) entram por config sem DDL; webhooks B2B sem consumo.
