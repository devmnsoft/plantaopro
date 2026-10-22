# R87a0 - LGPD + saúde: consentimento por escopo (prontuário/COAM), direitos do titular e retenção mínima legal

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R87 do bloco B4.
Escopo desta rodada: transformar a proteção de dados de saúde em regras executáveis do produto, por
cima dos dados clínicos já existentes (pacientes, consultas e documentos clínicos), sem nenhuma base
nova de armazenamento: consentimento específico e destacado por escopo controlado (PRONTUARIO e
COAM), os direitos do titular exercíveis dentro do produto (LISTAR/EXPORTAR/APAGAR) com trilha
auditável em `lgpd_eventos_privacidade`, e a retenção mínima legal do prontuário (20 anos a partir
do último registro) que bloqueia a eliminação com 409 dentro da janela, devolvendo a data em que ela
fica apta. APAGAR é executado como anonimização (LGPD art. 18, IV): nula os identificadores,
preserva o histórico clínico e grava um rótulo determinístico - não é eliminação física, porque as
~30 FKs do domínio clínico apontam para o paciente. Escopos, canais e direitos são catálogos
controlados na fonte única; sem nenhuma integração externa.

## Interpretação registrada: COAM

COAM = Comissão de Avaliação Médica: comissão interna de avaliação e revisão de casos (segunda
opinião, controle de qualidade assistencial). O recon não encontrou fonte pública conclusiva para
esse acróstico neste sentido (busca web registrada no recon desta rodada); pela convenção da casa, a
interpretação adotada pelo produto fica documentada aqui, amparada pela base citável: dados de saúde
são sensíveis (LGPD art. 5, II) e a assistência em si dispensa prévio consentimento (art. 7, VIII e
art. 11, II, "f") - mas compartilhar os dados do paciente com uma terceira comissão interna vai além
da assistência exclusiva, o que exige consentimento específico e destacado (art. 11, I). A semente
COAM no catálogo `lgpd_escopos_consentimento` carrega exatamente essa descrição e essa fonte.

## Fontes oficiais consultadas

- Lei 13.709/2018 (LGPD) - texto compilado conferido em planalto.gov.br durante o recon: art. 5, II
  (dado pessoal sensível inclui dado referente à saúde); art. 7, I (consentimento), II (cumprimento
  de obrigação legal) e VIII (tutela da saúde, exclusivamente, em procedimento realizado por
  profissionais ou serviços de saúde); art. 11, I (dado sensível: consentimento específico e
  destacado para finalidades específicas) e art. 11, II, "a" e "f" (dispensa de consentimento
  prévio: obrigação legal/regulatória e tutela da saúde exclusiva) - é essa dupla que torna a
  assistência dispensável de consentimento prévio e qualquer uso secundário ou compartilhamento com
  comissão interna exigente dele; art. 16 (eliminação após o término do tratamento, autorizada a
  conservação, entre outras finalidades, para cumprimento de obrigação legal - art. 16, I,
  fundamento da retenção mínima); art. 18 (direitos do titular: I confirmação da existência de
  tratamento, IV anonimização/bloqueio/eliminação, V portabilidade, VI eliminação dos dados
  tratados com consentimento).
- Lei 13.787/2018, art. 6: prazo mínimo de 20 anos para conservação do prontuário médico, em papel
  ou digitalizado, contado a partir do último registro - 240 meses, o valor aplicado ao escopo
  PRONTUARIO. Reprodução citável conferida no recon (artigo jurídico publicado no portal Migalhas).
- Resolução CFM nº 1.821/2007, arts. 7-8: prontuário em papel conservado por no mínimo 20 anos;
  prontuário eletrônico de forma permanente - os dois caminhos apontam para o mesmo piso de 20 anos
  a partir do último registro.
- CTN (Lei 5.172/1966), art. 194: prescrição quinquenal do crédito tributário - fundamenta os 60
  meses da categoria Financeiro (a linha já existia; a rodada completa `prazo_meses` e
  `fonte_oficial`).

## Regras implementadas

- `LgpdSaudeRegras` (aritmética pura, sem IO): soma de meses completos com truncamento
  determinístico do dia ao último dia do mês resultante (2004-01-31 + 1 mês = 2004-02-29); data
  apta = último registro + 240 meses; eliminação permitida apenas a partir da data apta (dia igual
  já cumpre os 20 anos); espelhos puros dos catálogos (escopos PRONTUARIO|COAM, canais
  RECEPCAO|TELEFONE|DIGITAL|PRONTUARIO, direitos LISTAR|EXPORTAR|APAGAR, base
  CONSENTIMENTO_EXPLICITO); validação do consentimento devolvendo todas as pendências de uma vez
  (escopo fora do catálogo, canal fora do catálogo, versão do termo obrigatória quando concedido);
  rótulo determinístico de anonimização ANONIMIZADO-<sha256(id)[..12]>; códigos padrão da trilha
  (LGPD_CONSENTIMENTO_REGISTRADO, LGPD_LISTAR_PACIENTE, LGPD_EXPORTAR_PACIENTE,
  LGPD_APAGAR_PACIENTE) e motivo RETENCAO_MINIMA.
- `LgpdSaudeService` (Dapper/Postgres, tudo escopado por tenant): listagem dos consentimentos do
  paciente por escopo (mais recente primeiro); registro/revoção com semântica de substituição -
  conceder marca o registro vigente anterior como SUBSTITUIDO e revogar como REVOGADO, entrando
  sempre um novo registro VIGENTE; envelope do paciente (nome, CPF, nascimento, e-mail, telefone,
  consentimentos e preview da retenção); exercício de direito deixando exatamente uma trilha por
  operação válida (detalhes em JSON com operador, IP, tenant, forma e fonte legal quando
  aplicável); "último registro" resolvido como a maior data entre consultas (início/finalização) e
  documentos clínicos emitidos. APAGAR: dentro da janela responde 409 com motivo RETENCAO_MINIMA e
  Data preenchida com a retenção (data apta na mensagem); já anonimizado responde 409; fora da
  janela nula os identificadores (nome para o rótulo, cpf/cns/nascimento/endereço para NULL,
  documento/e-mail/telefone para string vazia, bandeiras LGPD para falso) preservando consultas e
  documentos. Paciente fora do tenant responde 404 em toda superfície (não vaza existência).
- API `api/lgpd-saude` exigindo sessão autenticada: `GET consentimentos/{pacienteId}`,
  `POST consentimentos`, `GET dados/{pacienteId}`, `POST direitos/exercer` e
  `GET retencao/{pacienteId}`; falha de contrato = 400 com mensagem direta por pendência.
- Superfície web: 6 ações proxy `{ ok, data/erro }` no `LgpdController` existente (mesmo padrão das
  rodadas R82-R86; sem novo controller, o mapeamento de permissão Lgpd -> LGPD, a sidebar e os
  resx permanecem intocados) + página interativa de consentimentos, retenção e direitos sob o hub
  LGPD existente, com confirmação do apaga via modal (o contrato UX da casa proíbe
  alert/confirm nativos em view).

## Parâmetros semeados (migration v2370)

- Tabela `lgpd_escopos_consentimento` (catálogo institucional, padrão das fontes únicas do produto):
  2 sementes, PRONTUARIO (uso secundário além da assistência) e COAM (compartilhamento com a
  comissão interna), ambas com `exige_consentimento_explicito = true` e fonte oficial citável
  apontando a LGPD.
- `paciente_consentimentos` ganha `versao_termo varchar(40)` (obrigatória quando concedido=true) e
  `base_legal varchar(40) not null default 'CONSENTIMENTO_EXPLICITO'`; comentário da coluna
  `finalidade` registra que agora é escopo controlado validado contra o catálogo na camada de
  serviço.
- `lgpd_retencao_dados` ganha `prazo_meses integer` (check > 0) e `fonte_oficial text`; linha
  Prontuario com 240 meses e fonte 13.787/CFM 1.821 (insert where not exists) e linha Financeiro
  completada com 60 meses e fonte CTN art. 194 (update somente quando nulo).
- 6 linhas `LGPD_SA_*` em `parametros_legais`, cada uma com valor e fonte citável:
  `LGPD_SA_BASE_SENSIVEL` = CONSENTIMENTO_EXPLICITO; `LGPD_SA_ESCOPOS_CONSENTIMENTO` =
  PRONTUARIO|COAM; `LGPD_SA_CANAIS` = RECEPCAO|TELEFONE|DIGITAL|PRONTUARIO;
  `LGPD_SA_RETENCAO_PRONTUARIO_MESES` = 240; `LGPD_SA_RETENCAO_FISCAL_MESES` = 60;
  `LGPD_SA_DIREITOS` = LISTAR|EXPORTAR|APAGAR. Idempotente; manifesto v2370 com checksum SHA256
  real (LF `fd2c4569f2ad79c6481bcf7f7a5d09c10cb85def20e0b8c360e3d86eb75d2612`) e dependência em
  v2369; comentário da coluna `valor` amplia a convenção canônica com "CODIGO|CODIGO" para
  catálogos e meses inteiros para retenções mínimas.

## Exemplos travados em contrato (R87LgpdSaudeContratoTests, 8 fatos)

Vetores de retenção mínima (último registro = maior data entre consultas e documentos clínicos):

| vetor | último registro | data apta (+240 meses) | situação |
| --- | --- | --- | --- |
| antigo | 2005-01-15 | 2025-01-15 | eliminação apta |
| retenção ativa | 2015-06-30 | 2035-06-30 | bloqueada até 2035-06-30 |
| truncamento bissexto | 2004-01-31 + 1 mês | - | cai em 2004-02-29 |

Limite exato travado: 2025-01-14 ainda bloqueia; 2025-01-15 (dia igual à data apta) já libera.
Preview de retenção confere que, quando o documento (2004-06-01) é anterior à consulta
(2005-01-15), vence a maior data.

Consentimento: canal WHATSAPP cai com 400 apontando o campo Canal; concessão de dado sensível sem
versão do termo cai apontando Versão; escopo livre-texto MARKETING cai; concessões válidas nos dois
escopos normalizam caixa (recepcao -> RECEPCAO) e saem com base CONSENTIMENTO_EXPLICITO e status
VIGENTE; revogação marca o anterior REVOGADO e entra um novo VIGENTE não concedido; cada operação
válida deixa exatamente uma trilha LGPD_CONSENTIMENTO_REGISTRADO (3 operações válidas = 3 trilhas).

Direitos: LISTAR e EXPORTAR devolvem o envelope completo (nome, CPF, último registro,
consentimentos, preview da retenção com prazo 240 e fonte citável) e deixam uma trilha por
exercício; direito fora do catálogo (RETIFICAR) cai 400 antes de tocar no banco.

APAGAR: dentro da janela retorna 409 com erro RETENCAO_MINIMA, mensagem citando 2035-06-30 e Data
preenchida com a retenção, sem anonimizar nada e sem trilha de eliminação; fora dela, o paciente sai
com nome ANONIMIZADO-<12 hex> determinístico derivado do id, identificadores nulos, consulta
histórica intacta e trilha LGPD_APAGAR_PACIENTE com a fonte legal nos detalhes; segunda eliminação
responde 409 (já anonimizado).

Sem registros clínicos: último registro nulo, data apta nula e eliminação imediata (não há janela).
Paciente de outro tenant é invisível (404 em consentimentos e em exercício de direito - não vaza
existência).

Schema/seeds: migração v2370 idempotente na fonte única (reexecução não duplica: 2 escopos, 4
linhas de retenção ativas, 6 parâmetros), o parâmetro 240 confere com a constante pura
`PrazoProntuarioMeses`, e o manifesto traz hash real + dependência v2369. Padrão B6: advisory locks
r87_lgpd_saude_seed/_schema, seed/purge transacional com GUIDs fixos hex.

## Mudanças desta rodada

- `database/migrations/2026_10_v2370_r87_lgpd_saude.sql`: catálogo de escopos + colunas de
  consentimento (versão do termo, base legal) + colunas de retenção (prazo em meses, fonte oficial)
  + 6 parâmetros LGPD_SA_* idempotentes; comentários ampliam a convenção da coluna `valor`.
- `database/migration-manifest.json`: entrada v2370 (checksum SHA256 real, dependência v2369).
- `backend/PlantaoPro.Api/LgpdSaudeRegras.cs`: aritmética pura (meses com truncamento, data apta,
  limitadores, rótulo de anonimização, validações de consentimento).
- `backend/PlantaoPro.Api/LgpdSaudeService.cs`: serviço com consentimentos, envelope, exercício de
  direitos com trilha, APAGAR como anonimização com guarda da retenção mínima e preview.
- `backend/PlantaoPro.Api/Controllers/LgpdSaudeController.cs`: `api/lgpd-saude` com as 5 rotas
  acima; sessão autenticada exigida.
- `backend/PlantaoPro.Api/Program.cs`: DI no padrão da casa.
- `backend/PlantaoPro.Web/Controllers/LgpdController.cs`: 6 ações proxy `{ ok, data/erro }`
  (busca de paciente, consentimentos, registro de consentimento, retenção, exercício de direito).
- `backend/PlantaoPro.Web/Views/Lgpd/ConsentimentosSaude.cshtml`: busca de paciente,
  consentimento por escopo com canal rastreável e versão do termo, painel de retenção com data
  apta, direitos do titular (apaga com confirmação em modal) e bloco "Em linguagem simples" com as
  fontes citáveis.
- `backend/PlantaoPro.Web/Views/Lgpd/Index.cshtml`: card apontando para a nova tela dentro do hub.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo lgpd-saude-consentimentos
  (ordem 20, categoria privacidade) apontando para Lgpd/ConsentimentosSaude - a nova página entra
  já coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário após a mudança:
  páginas 680 (+1), artigos 88 (+1), cobertas 86 (+1), gap mantido em 517, nenhum artigo órfão,
  aux 29/pote quebra 29 estáveis.
- `backend/PlantaoPro.Tests/R87LgpdSaudeContratoTests.cs`: 8 fatos - manifesto v2370 com hash real
  e dependência; schema e seeds idempotentes na fonte única com fonte citável por linha; vetores de
  retenção com truncamento bissexto e limite exato; consentimento com as três rejeições, as duas
  concessões, a substituição/revoção e a trilha por registro; LISTAR/EXPORTAR com envelope e trilha;
  APAGAR bloqueado 409 com data apta, anonimizado fora da janela preservando o histórico e rejeitado
  na segunda vez; sem registros com eliminação imediata; isolamento de tenant.

## Suíte e gates

- Suíte cresce de 1737 para 1745 aprovados / 0 falhas (apps parados nas portas
  51976/51977/52976/52977 antes da execução).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA (dedupe: 76 únicos de cada lado; logs `%TEMP%\opencode\r87web_build_base.txt` /
  `r87web_build_nova.txt`).
- Gap de manuais mantido travado em 517 (nova página + novo artigo no mesmo ciclo; inventário
  versionado atualizado, nenhum artigo órfão).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final: 0 defeitos.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R88 - trabalhista: eventos eSocial por colaborador (S-1000...S-1299) em preview, horas/jornada,
aviso prévio.
