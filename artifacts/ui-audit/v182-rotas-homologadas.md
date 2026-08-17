# Rotas — homologação v1.82.0

As 24 rotas críticas estão cadastradas no smoke: `/`, `/Account/Login`, `/cadastro/empresa`, `/Planos`, `/AdminSaas/Index`, `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia`, `/Agenda`, `/Agendamentos`, `/Saude360`, `/Pacientes`, `/Triagem`, `/Consultas`, `/FaturamentoClinico`, `/Financeiro`, `/Pagamentos`, `/Plantoes`, `/Escalas`, `/Fechamentos`, `/Relatorios`, `/Configuracoes` e `/MinhaAssinatura`.

- **Verificação estática: APROVADA** pelos gates de controllers, layout, SaaS e UX operacional.
- **Homologação HTTP/runtime: BLOQUEADA** porque `dotnet` não existe no ambiente.
- **Rotas autenticadas: BLOQUEADAS** adicionalmente porque `artifacts/auth/storage-state.json` não existe.

Executar após startup:

```bash
export PLANTAOPRO_BASE_URL="http://localhost:5000"
export PLANTAOPRO_STORAGE_STATE="artifacts/auth/storage-state.json"
node scripts/ui/visual-smoke.mjs
```
