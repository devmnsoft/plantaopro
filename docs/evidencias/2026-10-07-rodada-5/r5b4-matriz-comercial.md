# R5-B4 — Matriz comercial canônica + estados explícitos

Data: 2026-10-08. Escopo: plano→módulos→capacidades→limites→recursos adicionais
(fonte única), estados explícitos (TRIAL, vigência, agendamento, ativação,
suspensão, cancelamento, upgrade/downgrade, limite-zero, adicionais).

## GATE A (pré-requisito do bloco — verificado)

- Auth/sessão: POSTs anonimos/diretos seguem 401/302-login; guard por ação no
  fiscal (R5-A2) e no comercial (AUDITOR barrado nos POSTs, leitura liberada).
- Tenant/perfil: isolamento cross-tenant nos testes; contratos por tenant.
- Sem sucesso fictício: Emitir honesto (R5-A2); solicitações de plano mostram
  "aguardando avaliação", nunca "concluído"; catálogo público sem preços
  inventados (lê o banco; vazio honesto em falha).
- Navegação: smoke Web real como gestor Santa Casa (Notas/Configurar/Detalhes
  200; MinhaAssinatura/Uso/Upgrade 200 com dados reais).
- Testes: suíte completa **1135/1135** (2 runs verdes seguidos). Runs
  intermediários falharam 1–2 testes de delta global da classe
  Saude360R4B9 (g1-g0 sensível a escritores paralelos); investigação: passam
  isolados, árvore limpa sem B4 também falhou 1× e zerou 2×, árvore B4 zerou
  2× — flake de paralelismo pré-existente (mesma classe dos flakes de backlog),
  sem relação com o B4. Nenhum teste B4 falha.

## Defeitos reais encontrados e corrigidos (todos provados no banco real)

Colisão de migrations ("IF NOT EXISTS congela o shape de quem rodou
primeiro") — cada uma quebrava um comando crítico com 500/42804/42703:

1. `planos.publico/destaque/permite_white_label` TEXT vs boolean, `ordem` TEXT
   vs int → `api/planos/publicos` 500 para todo mundo. **v2323**: conversão
   com USING (NULL→false/999, documentado); endpoint voltou (4 planos reais).
2. `upgrade/downgrade_solicitacoes` sem colunas ricas → solicitar-upgrade/
   downgrade/cancelamento 500. **v2323**: ADD COLUMNs + decisão
   (decidido_por/em/justificativa).
3. `plano_modulos` com 3 shapes; código (LinkPlan/matriz) vs banco.
   **v2324**: colunas canônicas + índice único do ON CONFLICT.
4. `plano_recursos` sem nome/descricao → GET/PUT recursos 42703. **v2325**.
5. `assinatura_historico` sem colunas do ALTERAR_PLANO (+status_novo NOT NULL,
   +reg_status) → alterar-plano manual B2B 500 **desde sempre** (nunca
   funcionou em banco real). **v2326+v2327** + fix do insert manual.
6. Web: `MinhaAssinatura/Index` desserializava o DTO no VM errado (tela sempre
   vazia); `Uso/Limites/Faturas` hardcoded (35/100, R$899); `Upgrade/Downgrade`
   com botões mortos e preços estáticos inventados (399/899/1999 fora do
   banco); `Cancelamento` POST 405. Tudo ligado nos endpoints reais.
7. Solicitações de upgrade/downgrade/cancelamento eram dead-end (ninguém lia).
   Novos endpoints admin `api/admin-saas/solicitacoes-planos` (listar +
   aprovar/recusar por tipo, idempotente, lock por cliente, revalidação de
   uso no downgrade, trilha ALTERAR_PLANO) + `MinhasSolicitacoes` no cliente.
8. `tenant_modulos` AGENDADO sem ativação: `POST api/admin-saas/modulos/
   ativar-agendados` (idempotente + trilha; scheduler = pendência E13).

## Novos endpoints/API

- `GET api/planos/{id}/matriz` (Global/Admin): plano + módulos
  (incluído/limite/preço adicional) + recursos + regras explícitas
  (limite-zero=ilimitado, vigência/TRIAL barrados na leitura, AGENDADO exige
  ativação, carência = decisão pendente).
- `GET/POST api/admin-saas/solicitacoes-planos/...` (Global): ver acima.
- `GET api/minha-assinatura/solicitacoes` (cliente).
- `POST api/admin-saas/modulos/ativar-agendados` (Global).

## Testes novos (20+8)

- `SaasComercialB4MatrizTests` (12, API+DB): matriz 200/404; aprovar upgrade
  (troca + trilha + idempotência); destino inativo 409; recusar 400/ok;
  cancelamento aprovado; coerência tipo×status 409; inexistente 404;
  downgrade 409/ok; ativar-agendados; listagem com filtros.
- `SaasComercialB4MinhaAssinaturaTests` (8, BFF/stub): Index mapeia;
  Uso ilimitado; AUDITOR barrado; Upgrade lista+pendente; POST upgrade;
  cancelamento curto (sem API) e válido; /planos anônimo do banco.

## Achado extra no smoke (corrigido)

`GET api/minha-assinatura` para tenant sem cliente vinculado devolvia
envelope 200-falso (`success:false/message:"Sucesso"` — o `Fail(ctx.Message,
ctx.StatusCode)` herdava 200/"Sucesso" do contexto). Corrigido em
`MinhaAssinaturaAsync`/`ObterUsoPlanoAsync`/`MinhasSolicitacoesAsync`: sem
cliente/tenant → 404 honesto ("Organização sem cliente vinculado..."),
propagado por uso/faturas/solicitações. Web mostra vazio honesto.
Smoke: santa-casa demo (sem assinatura — lacuna de demo p/ B5) → Index/Uso
200 honestos; Upgrade//planos listam os 4 planos reais do banco.

## Decisões pendentes (NÃO inventadas)

- Carência: sem modelo — pendente.
- `assinatura_modulos`: tabela existe, nenhum código usa — pendente (B5/C?).
- Scheduler para `ativar-agendados` (E13); TRIAL padrão de novos cadastros
  (B5); status VENCIDA da coluna (enforcement é na leitura — ok por ora).
- `publico` NULL→false e `ordem` NULL→999: planos internos seguem ocultos
  até decisão comercial explícita (PUT planos).
- Billing admin (BillingController placeholders sobre api/billing/*) e
  provider de cobrança: B6.
- Cadastro.Confirmar → finalizar: B5 (lista de planos do cadastro já é real).
