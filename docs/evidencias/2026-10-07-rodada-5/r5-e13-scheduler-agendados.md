# R5-E13/P2 — Scheduler de contratos AGENDADO (AGENDADO→ATIVO sem chamada manual)

Data: 2026-10-09. Contexto: backlog P2 da matriz de aceite (`r5-entrega-matriz-aceitacao.md` §3).
Desde B4 a transição AGENDADO→ATIVO de upgrade com início futuro só ocorria por
`POST api/admin-saas/modulos/ativar-agendados` (rota manual, ADMINISTRADOR_GLOBAL); um contrato
vencido continuava inoperante "por esquecimento operacional", o que é defeito de produto.

## 1. O que foi implementado

| Camada | Arquivo | Mudança |
|---|---|---|
| Kernel | `backend/PlantaoPro.Api/SaasCoreServices.cs` | `AtivarAgendadosAsync` (rota) mantém o gate `IsGlobalAdmin` e delega ao núcleo privado `AtivarVencidosAsync(usuarioId, ip, ct)`; nova entrada `AtivarAgendadosProgramadoAsync` chama o **mesmo núcleo transacional** com ator do sistema (`usuario_id NULL`, `ip_origem 'SCHEDULER'`) — o tick nunca finge ser usuário logado; auditoria só quando há usuário (trilha canônica do tick é `tenant_modulos_historico`) |
| Scheduler | `backend/PlantaoPro.Api/SaasModulosAgendadosHostedService.cs` | `BackgroundService` no padrão B7: pulado no ambiente `Testing` (suite xUnit compartilha o banco), `PeriodicTimer`, escopo DI por tick (serviço é scoped), tick falho é logado e retomado (não derruba o host). Config `Saas:AtivarAgendados:IntervaloSegundos` (default 300 s; `<= 0` desliga) |
| Host | `backend/PlantaoPro.Api/Program.cs` | `AddHostedService<SaasModulosAgendadosHostedService>()` junto ao recovery B7 |

Propriedades de segurança herdadas do kernel (não duplicadas no scheduler): só
`AGENDADO AND ativado_em <= now()` transiciona (estado terminal nunca rebaixado), `FOR UPDATE`
serializa instâncias concorrentes (a segunda reavalia o predicado e não encontra mais a linha),
tick sem vencidos é no-op — logo sobreposição/repetição são inofensivas.

Contrato travado por teste: `backend/PlantaoPro.Tests/SaasAgendadosSchedulerContractTests.cs`
(3 Facts: rota mantém gate 403 e ambos usam um único núcleo; scheduler pula Testing/lê config/
cria escopo/não derruba host; registrado na host). Total da suite: 1175 → **1178**.

## 2. Suite automatizada (hosts down)

`dotnet build` limpo; `dotnet test` completo: **1178/1178 verdes** (inclui B4 matriz 12/12 e
B5 provisionamento 11/11 no filtro direcionado, sem regressão no caminho compartilhado).

## 3. Loop ao vivo (Development, API com intervalo 10 s via env)

Fixture canônico criado por INSERT direto (estado agendado real da tabela, módulo AGENDA do
catálogo), todo o resto por dado persistido:

```
SEED_AGENDADO_ID=0815e1cb-914c-4c77-a01a-43ce664908a1
AGENDADO|f|t                     # status AGENDADO, habilitado=false, ativado_em vencido (2 min atrás)
--- apos ticks (~35 s, 3+ ticks de 10 s) ---
ATIVO|t                          # virou ATIVO e habilitado=true SEM nenhuma chamada de rota
ATIVACAO_AGENDADA|t|SCHEDULER    # trilha: acao, usuario_id IS NULL, ip_origem='SCHEDULER'
LIMPEZA_OK                       # fixture removido; contratos do tenant demo permanecem os reais
```

Leitura do banco é a única fonte de conclusão. O log de console da API captura 0 bytes neste
ambiente (limitação conhecida do redirecionamento em `dotnet run`), portanto a evidência de
autonomia é a própria trilha persistida com ator do sistema — que inclusive discrimina o scheduler
da rota manual (nesta teria `usuario_id` do superadmin + IP do pedido).

## 4. Limites conhecidos (sem maquiagem)

- A rota manual continua existente e útil para ativação imediata pelo superadmin; o scheduler é a
  porta automática com o mesmo semântica, não uma segunda máquina de estados.
- Em `Testing` o scheduler fica mudo por construção; a cobertura ali é a suite B4/B5 + estes
  contratos de fonte.
- Multi-instância real (IIS/web-farm) ainda contará apenas com `FOR UPDATE` como serialização —
  suficiente para correção (idempotência verificada em B5); métrica/lock advisory fica opcional.
