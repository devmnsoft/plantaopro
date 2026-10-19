# Plano 50 sprints (R66–R115) — "maior e melhor SaaS do segmento"

Data: 2026-10-19 · Diretriz do usuário: banco em **um único script de importação
sem erro**; design responsivo sem confundir **cor de função com cor da letra**;
textos **compreensíveis para leigos**; **manual em toda página**; regras de negócio
funcionando sob **legislação brasileira real** (e viáveis fora do país); zero
mensagens que sugiram homologação; seguir o plano por **mais 50 sprints sem parar**.

Base: `33a9fda` (fila r53b1 §7 fechada em R65; suíte 1650). Cada sprint = rodada
canônica: recon → mudança → evidência versionada → gates (build Release
`--no-incremental`, gate de casa NENHUMA, suíte integral limpa, smoke R52 quando
apps entram, docscan) → 2 commits ASCII → push `origin main`.

## Blocos e rodadas

### B1 — Banco em importação única (R66–R68)
- **R66**: prova de importação zero-erro do `scrpt_completo.sql` em banco vazio
  novo (`ON_ERROR_STOP=1`, saídas 100% verde), paridade exata do conjunto de tabelas
  vs banco sábio local, assert de seeds de sistema + seção R62 (drael), script
  reexecutável na pasta da rodada.
- **R67**: instalador completo `instalar_plantaopro.psql` do início ao fim
  (roles+banco+schema+seed sistema+bootstrap admin) em ambiente limpo; guia de
  instalação de 1 comando documentado (local/demo/cloud); variante `pgadmin/
  instalar_no_banco_atual.sql` provada equivalente.
- **R68**: perfis de importação (base pura vs com demo opcional) + reimportação
  idempotente (2ª aplicação sem erro) + contrato travando o padrão.

### B2 — Design, cores, legibilidade (R69–R74)
- **R69**: auditoria WCAG AA de contraste (texto/fundo em telas densas, alertas,
  badges, tabelas) + correções de tokens; nunca cor de função sobreposta à cor
  da letra de forma ambígua.
- **R70**: varridura responsiva (360/480/768/1024 px): forms, modais e tabelas
  densas sem estouro/truncamento; correções.
- **R71**: semântica de cores: todo status sempre ícone+texto (nunca só cor),
  mapa único de estado→cor→ícone aplicado onde divergir.
- **R72**: template unificado de formulários (label claro, ajuda curta, erro em
  linguagem simples ao lado do campo, obrigatórios sinalizados) aplicado aos gaps.
- **R73**: linguagem simples v2: varredura de jargão nos resx/views restantes;
  substituição por texto que leigo entende (contratos atualizam as strings exatas).
- **R74**: guardas de design: contrato travando o mapa de tokens + varredura
  anti-padrões (cores de função sem texto, contraste < AA conhecido).

### B3 — Manual em TODA página (R75–R79)
- **R75**: inventário máquina páginas×artigos (gap medido hoje: ~25 artigos para
  ~319 ações de view); script de gap versionado.
- **R76**: lotes de artigos — núcleo clínico (triagem, agendamento, consulta,
  longitudinal, receitas/exames).
- **R77**: lotes — Administrativo 360 (cadastros, estoque, compras, vendas,
  faturamento, financeiro, compras/recebimentos).
- **R78**: lotes — SaaS/Centrais (planos, usuários/perfis, notificações, cobranças,
  ajuda da ajuda) + botão "Ver manual" padronizado presente em todo header.
- **R79**: contrato: zero página sem artigo correspondente + busca da Ajuda cobrindo
  termos usados nas guias das telas.

### B4 — Legislação brasileira funcionando (R80–R90)
- **R80**: mapa legislativo×código: o que já existe (DF-e mod-55, cobranças sandbox,
  relatórios gerenciais) vs o que falta por domínio; tabela `parametros_legais`
  (valor, fonte oficial, vigência, ano) como fonte única.
- **R81**: núcleo de tributação pessoa física 2026: INSS progreda, IRRF, teto,
  mínimo — contrato com vetores oficiais.
- **R82**: folha de pagamento funcional: 13º, férias+1/3, horas extras, rescisão
  (verbas) — serviço + tela simples de cálculo explicada em linguagem leiga.
- **R83**: Simples Nacional: classes de receita, faixas cumulativas, preview de DAS
  com exemplo real e explicação campo a campo.
- **R84**: calendário fiscal/obrigações legais (DAS dia 20, FGTS dia 20, DCTFWeb dia
  15, eSocial, EFD, livros contábeis) → motor de obrigações/reminders por tenant.
- **R85**: DF-e nacional: configuração SEFAZ por UF, layouts (NF-e 4.0, NFC-e,
  CF-e-SAT, CF-e-Serviços 3.0), chave de acesso mod-11 com DV por UF.
- **R86**: trilhos de pagamento: remessa CNAB 400/240, FEBEN, PIX EMV (validação+
  geração) amarrados ao motor de cobrança.
- **R87**: LGPD + saúde: consentimento (COAM/prontuário), direitos do titular
  (listar/exportar/apagar com trilha), retenção mínima legal.
- **R88**: trabalhista: eventos eSocial por colaborador (S-1000…S-1299) em preview,
  horas/jornada, aviso prévio.
- **R89**: venda para fora do país: moeda base BRL + cotações (USD/EUR/GBP),
  faturamento internacional simplificado (nota fiscal não-tradicional/VAT por
  destino) mantendo i18n dos 4 locales.
- **R90**: bateria de contratos legais: parâmetros oficiais 2026 (mínimo R$ 1.518,
  tetos e tabelas INSS/IRRF) travados com fonte citada; exemplos reais de cálculo.

### B5 — Escala, performance e confiabilidade (R91–R95)
- **R91**: paginação/virtualização das listas densas (Orçamentos 17,2 MB, Estoque
  8,75 MB — pendência 1 do §7 ainda aberta).
- **R92**: planos de execução EXPLAIN nos caminhos mais quentes + índices faltantes.
- **R93**: observabilidade: erros de negócio centralizados + digest diário.
- **R94**: carga/sustentação (script PS/k6): thresholds documentados.
- **R95**: drill de backup/restore usando o script único (B1) como artefato.

### B6 — Liderança comercial e produto (R96–R105)
- **R96**: onboarding self-service (assinatura → tenant pelo motor existente → trial).
- **R97**: planos/preços configuráveis + entitlements (plano HOMOLOGACAO_360 etc.).
- **R98**: assistente Drael em linguagem simples (resumo do pendente, orientação ao
  usuário final).
- **R99**: jornadas guiadas para tenants novos (checklist por módulo).
- **R100**: notificações pautadas no calendário fiscal (R84) + SLAs + escalonamento.
- **R101**: relatórios com benchmark ("seus indicadores vs o segmento" por plano).
- **R102**: experiência PWA para triagem/recepção (fluxo crítico no celular).
- **R103**: integrações: WhatsApp (opt-in), e-mail transacional, API pública com keys.
- **R104**: segurança: política de senha, MFA TOTP, auditoria de sessões.
- **R105**: disponibilidade: health interno, alertas, página de manutenção.

### B7 — Excelência contínua e entrega final (R106–R115)
- **R106**: E2E das jornadas críticas (triagem→consulta→fatura→recebimento;
  pedido→recebimento→contas a pagar; emissão).
- **R107**: auditoria de nível de leitura do conteúdo (mensagens críticas travadas
  por contrato).
- **R108**: auditoria visual pós-mudanças + baseline de regressão.
- **R109**: internacionalização de regimes fiscais (primeiros destinos EUA/EU/MX,
  regras reais mínimas + configuração).
- **R110**: régua contábil caixa×competência nos relatórios gerenciais.
- **R111**: auditoria de qualidade de dados (órfãos/statuses/valores) + jobs de cura.
- **R112**: TTFB e cache de assets (versão atual v2151).
- **R113**: DR: PITR ensaiado, RTO/RPO documentados.
- **R114**: retrospectiva 50 sprints: antes/depois medido + backlog seguinte.
- **R115**: release final: tag, notas do instalador, resumo executivo.

## Regras transversais (valem em todas as 50)
1. Mensagem para usuário: jamais sugerir homologação/aprovação formal (regra R57 +
   whitelist existente preservada).
2. Todo texto novo em 4 locales (pt/en/es/fr) com paridade travada por R7E1.
3. Cor nunca é o único canal de informação de estado (B2 vira regra permanente).
4. Toda regra de negócio legal traz fonte oficial + vigência em `parametros_legais`.
5. Suíte cresce monotonamente; qualquer falha segue o catálogo R65 (nome capturado,
   isolado 5×, classificado).
