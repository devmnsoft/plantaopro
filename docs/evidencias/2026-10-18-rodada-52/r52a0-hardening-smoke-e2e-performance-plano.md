# R52 · r52a0 — Hardening: smoke e2e completa + performance percebida

Data: 2026-10-18 · Sessão: `opencode` · Base: R51 (`7dbf1ec`, suíte 1605).

## Objetivo

Rodada de hardening (sem mudança funcional): uma **smoke e2e completa** scriptada e versionada que
cobre as jornadas principais em API+Web (inclusive as áreas novas da R51), mais uma **linha de base de
performance percebida** (tempo de página em ambiente dev local).

## Escopo

### 1. Smoke e2e (`r52_smoke_e2e.ps1` versionada nesta pasta)
Fluxo completo contra `plantaopro_test` com apps reais:
1. API (`http://localhost:51976`) e Web (`http://localhost:51977`) subidas em Release `--no-build`.
2. Login demo do gestor (`gestor@santacasa-demo.example`) via form + antiforgery (padrão R49).
3. Jornada Web por área (cada página deve responder 200 com marcador esperado):
   - Minha Central (resumo + alerta de inadimplência se houver fatura vencida — R48);
   - Administrativo360: Vendas, Orçamentos, Pedidos de Compra, Estoque, Títulos a Receber,
     Relatórios Financeiros;
   - Central de Ajuda: Index (topicos visíveis para o perfil ADMINISTRADOR_CLIENTE — incluindo o novo
     "Operação da instituição"), Artigo `adm-orcamento`, Artigo `central-fatura`, Modulo `instituicao`;
   - Notificações/Preferências.
4. Verificação cruzada de acessibilidade (R49): `aria-label=` presente nas páginas autenticadas,
   v2151 + accessibility.css carregados nos layouts.
5. Teardown: apps parados, portas 51976/51977 livres (estado de casa entre rodadas).

### 2. Performance percebida (linha de base)
- Mesmo script cronometra cada GET autenticado (amostra única por página, ambiente dev local);
- orçamento registrado como referência: **média < 3.000 ms** por página em loopback (máquina dev,
  primeira carga pós-login); números exatos entram no r52z.
- Sem mudança de infra nesta rodada: otimização só se algum número estourar o orçamento (registro de
  pendência com causa provável).

## Gates

Build real + gate 160 chaves (proteção contra regressão mesmo sem mudança de código-fonte); suíte
integral (esperado 1605 estável); commits `docs (rodada 52 r52b1)` (script versionado + evidência) se
houver ajuste de código, senão apenas `docs (rodada 52 r52z)`; push.
