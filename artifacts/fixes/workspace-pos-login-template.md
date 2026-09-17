# Workspace pós-login

## Login

O PR #496 já está no `main`. As contas locais são:

- `superadmin@mnsoft.example` / `MnSoft!Demo2026#Admin`
- `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor`

Execute `database/seeds/development/120_acesso_demo_local.sql` no banco `plantaopro` se ainda não houver usuários.

## Evolução desta rodada

- `Home/Index` encaminha o superadmin ao Command Center e o gestor do cliente ao Meu Dia.
- Command Center deixa de exigir papéis legados `Admin,Gestor` e passa a usar `RolesConstants.Operacao` (inclui `ADMINISTRADOR_GLOBAL` e `ADMINISTRADOR_CLIENTE`).
- Meu Dia ganha cartões das jornadas Escalas, Execução e Conferência, alinhados ao seed demo.
- Command Center ganha trilha de atalhos por perfil e CTA para Escalas no estado vazio.
