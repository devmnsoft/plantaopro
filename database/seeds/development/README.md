# Seeds de desenvolvimento

Este diretório é deliberadamente opt-in. O instalador canônico nunca cria administrador com senha conhecida em produção.

Em Development a API agora auto-provisiona as contas se `DemoSeed:Enabled` e `DemoSeed:AutoProvisionIfEmpty` estiverem ativos.

## Contas locais (somente Development)

| Perfil | E-mail | Senha |
|---|---|---|
| Super administrador | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` |
| Gestor da Santa Casa Demonstração | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` |
| Médica fictícia | `medico@santacasa-demo.example` | `Medico!Demo2026#Acesso` |

## SQL manual

```bash
# 1. Provisionar acesso demo das 3 contas (superadmin, gestor, medica)
psql -d plantaopro -f database/seeds/development/121_acesso_demo_local.sql

# 2. Provisionar jornada demonstrável da Santa Casa (Escalas -> Execução -> Conferência)
psql -d plantaopro -f database/seeds/development/130_operacao_demo_santacasa.sql
```

Se a API estiver no banco `postgres` legado, execute os mesmos arquivos nesse banco — desde que o schema `plantaopro` exista.

### Conteúdo do seed de operação (130_operacao_demo_santacasa.sql)

Garante que o tenant `santa-casa-demonstracao` possua:
- Módulos contratados: `ESCALAS`, `EXECUCAO`, `CONFERENCIA`.
- Especialidade `Clínica Médica` e Setor `Pronto Socorro Adulto`.
- 4 plantões na janela `[ontem, hoje, amanhã, +3 dias]`:
  - **Ontem:** plantão realizado pela médica demo com fechamento em conferência.
  - **Hoje:** plantão confirmado com check-in ativo da médica demo e ocorrência aberta.
  - **Amanhã:** plantão noturno com 1 vaga atribuída à médica e 1 vaga em convite de cobertura.
  - **+3 dias:** plantão diurno sem médico escalado (descoberto para ação do gestor).

## Diagnóstico

```sql
SELECT email, status, reg_status, left(senha_hash,7) AS hash_prefix
FROM plantaopro.usuarios
WHERE lower(email) IN (
  'superadmin@mnsoft.example',
  'gestor@santacasa-demo.example',
  'medico@santacasa-demo.example'
);
```
