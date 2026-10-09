# R5-D10 — Loop de onboarding fechado ao vivo (dados da empresa → convite → aceite → reavaliação)

Data: 2026-10-09. Ambiente: hosts locais (API 51976 / Web 52976) → banco `plantaopro_test`.
Sem browser automation: probes PowerShell (`d10_*.ps1`) contra HTTP real + verificação via psql.
Nenhuma linha inserida "na mão" nesta rodada — todas as escritas passaram por rotas do produto.

## 1. Passos executados por rotas reais

1. **Dados da empresa completos** — superadmin `PUT api/clientes/d3f6584c-…7501` (área Clientes do
   Web usa a mesma rota) preenchendo e-mail corporativo, telefone, cidade e UF persistidos em
   `plantaopro.clientes`. O avaliador `DADOS_EMPRESA_COMPLETOS` relê o cadastro (inclusive fallback
   de cidade em `cadastro_cliente_solicitacoes`) e inverte a etapa sozinho.
2. **Convite de equipe** — gestor logado no Web → `POST /ConvitesEquipe/Criar`
   (`enfermeiro.demo@santacasa-demo.example`, perfil, validade em dias). Token de 32 bytes hex
   exibido **uma vez** (TempData no Index); o banco guarda apenas o hash SHA-256
   (`usuario_convites.token_hash`) — confirmado via psql.
3. **Aceite anônimo** — `GET/POST /convite/aceitar/{token}` sem sessão: cadastro da senha
   (`Convite!Demo2026#Aceito`) e ativação do usuário convidado; aceite marca o convite e vincula o
   usuário ao tenant (origem do gate `MEMBRO_EQUIPE_ATIVO`).
4. **Reavaliar** — `POST /Onboarding/Reavaliar` (gestor) recalcula tudo a partir dos dados
   persistidos: `ONB_EMPRESA_DADOS`, `ONB_EQUIPE_CONVIDAR` e `ONB_EQUIPE_ACESSO` viraram
   `CONCLUIDO` com origem `AUTOMATICO` e evidencia textual do dado encontrado.

## 2. Estado medido após o loop

- Checklist: **8 de 12** `CONCLUIDO` (núcleo 3 + PLANTOES 3 + ADM360 1 + …) — todos os concluídos
  com `origem_conclusao='AUTOMATICO'`; nada foi "clicado para concluir".
- Prova SQL das três viradas com a evidencia de cada critério (convite registrado / token usado /
  cadastro completo).

## 3. Observações (sem maquiagem)

- O aceite de convite é a única superfície anônima tocada pelo loop; o token não reutilizável e a
  expiração já tinham testes de contrato (B5) e foram reconfirmados pelo fluxo real.
- Vícios de ambiente encontrados durante o loop (consumidos por redirects `-L`): banners TempData
  de sucesso/erro só aparecem na página seguinte à redireção — os probes leem a página final.
